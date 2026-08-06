import ctypes
import json
import os
import subprocess
import sys
import tempfile
import threading
import time
import tkinter as tk
import tkinter.simpledialog
from tkinter import messagebox, ttk


APP_TITLE = "Limpador / Formatador de HD e SSD USB"
REFRESH_SECONDS = 3
FAT32_MAX_GB = 32


def is_admin() -> bool:
    try:
        return bool(ctypes.windll.shell32.IsUserAnAdmin())
    except Exception:
        return False


def relaunch_as_admin() -> None:
    """Reabre o próprio app como administrador uma única vez."""
    if is_admin():
        return

    params = " ".join([f'"{arg}"' for arg in sys.argv])
    ctypes.windll.shell32.ShellExecuteW(
        None,
        "runas",
        sys.executable,
        params,
        None,
        1,
    )
    sys.exit(0)


def run_hidden(command: list[str], timeout: int | None = None) -> subprocess.CompletedProcess:
    startupinfo = subprocess.STARTUPINFO()
    startupinfo.dwFlags |= subprocess.STARTF_USESHOWWINDOW

    return subprocess.run(
        command,
        capture_output=True,
        text=True,
        timeout=timeout,
        startupinfo=startupinfo,
        creationflags=subprocess.CREATE_NO_WINDOW,
        encoding="utf-8",
        errors="replace",
    )


def get_usb_disks() -> list[dict]:
    """
    Lista discos USB usando PowerShell/Get-Disk.
    Retorna apenas discos USB e calcula tamanho total, espaço usado, livre e letras de unidade.
    """
    ps_script = """
$disks = Get-Disk | Select-Object Number,FriendlyName,SerialNumber,BusType,Size,PartitionStyle,OperationalStatus,HealthStatus,IsBoot,IsSystem,IsOffline,IsReadOnly
$result = foreach ($d in $disks) {
    if ($d.BusType -eq 'USB' -and -not $d.IsBoot -and -not $d.IsSystem) {
        $parts = Get-Partition -DiskNumber $d.Number -ErrorAction SilentlyContinue
        $vols = $parts | Get-Volume -ErrorAction SilentlyContinue
        
        $driveLetters = ($vols | Where-Object { $_.DriveLetter } | ForEach-Object { $_.DriveLetter + ":" }) -join ", "
        
        $totalSize = [int64]($d.Size)
        $remSize = 0
        $hasVols = $false
        if ($vols) {
            $hasVols = $true
            foreach ($v in $vols) {
                if ($null -ne $v.SizeRemaining) {
                    $remSize += [int64]$v.SizeRemaining
                }
            }
        }

        [PSCustomObject]@{
            Number            = $d.Number
            FriendlyName      = $d.FriendlyName
            SerialNumber      = $d.SerialNumber
            BusType           = $d.BusType
            Size              = $d.Size
            SizeRemaining     = if ($hasVols) { $remSize } else { $null }
            DriveLetters      = $driveLetters
            PartitionStyle    = $d.PartitionStyle
            OperationalStatus = $d.OperationalStatus
            HealthStatus      = $d.HealthStatus
            IsBoot            = $d.IsBoot
            IsSystem          = $d.IsSystem
            IsOffline         = $d.IsOffline
            IsReadOnly        = $d.IsReadOnly
        }
    }
}
$result | ConvertTo-Json -Depth 4
"""
    result = run_hidden(
        ["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", ps_script],
        timeout=15,
    )

    if result.returncode != 0 or not result.stdout.strip():
        return []

    try:
        data = json.loads(result.stdout)
    except json.JSONDecodeError:
        return []

    if isinstance(data, dict):
        data = [data]

    usb_disks: list[dict] = []
    for disk in data:
        if not is_disk_eligible(disk):
            continue

        size_bytes = int(disk.get("Size") or 0)
        disk["SizeGB"] = round(size_bytes / (1024 ** 3), 2) if size_bytes else 0

        rem_bytes = disk.get("SizeRemaining")
        if rem_bytes is not None and size_bytes > 0:
            rem_bytes = int(rem_bytes)
            used_bytes = max(0, size_bytes - rem_bytes)
            used_gb = round(used_bytes / (1024 ** 3), 2)
            free_gb = round(rem_bytes / (1024 ** 3), 2)
            pct = round((used_bytes / size_bytes) * 100, 1) if size_bytes else 0
            disk["UsedGB"] = used_gb
            disk["FreeGB"] = free_gb
            disk["UsedStr"] = f"{used_gb} GB ({pct}%)"
            disk["FreeStr"] = f"{free_gb} GB"
        else:
            disk["UsedGB"] = None
            disk["FreeGB"] = None
            disk["UsedStr"] = "Não alocado"
            disk["FreeStr"] = "0 GB"

        usb_disks.append(disk)

    usb_disks.sort(key=lambda item: int(item.get("Number", 9999)))
    return usb_disks


def is_disk_eligible(disk: dict) -> bool:
    """Retorna True apenas para discos USB seguros para apagar."""
    bus_type = str(disk.get("BusType", "")).upper()
    if bus_type != "USB":
        return False
    if disk.get("IsBoot") or disk.get("IsSystem"):
        return False
    if disk.get("IsReadOnly"):
        return False
    if disk.get("IsOffline"):
        return False
    return True


def disk_block_reason(disk: dict) -> str:
    """Explica por que um disco não pode ser processado."""
    number = disk.get("Number", "?")
    bus_type = str(disk.get("BusType", "")).upper()
    if bus_type != "USB":
        return f"Disco {number} não é USB."
    if disk.get("IsBoot"):
        return f"Disco {number} é disco de boot."
    if disk.get("IsSystem"):
        return f"Disco {number} é disco de sistema."
    if disk.get("IsReadOnly"):
        return f"Disco {number} está em somente leitura."
    if disk.get("IsOffline"):
        return f"Disco {number} está offline."
    return ""


def validate_selected_disks(selected: list[int]) -> tuple[list[dict], str | None]:
    """Revalida discos imediatamente antes de executar o diskpart."""
    disks = get_usb_disks()
    by_number = {int(d.get("Number")): d for d in disks}

    validated: list[dict] = []
    for number in selected:
        disk = by_number.get(number)
        if disk is None:
            return [], f"Disco {number} não está disponível ou deixou de ser USB."
        reason = disk_block_reason(disk)
        if reason:
            return [], reason
        validated.append(disk)

    return validated, None


def save_error_log(content: str) -> str:
    """Salva log completo do diskpart em arquivo temporário."""
    log_dir = os.path.join(os.environ.get("TEMP", "."), "Limpador_USB_logs")
    os.makedirs(log_dir, exist_ok=True)
    log_path = os.path.join(log_dir, f"diskpart_{time.strftime('%Y%m%d_%H%M%S')}.log")
    with open(log_path, "w", encoding="utf-8") as f:
        f.write(content)
    return log_path


def diskpart(script_lines: list[str]) -> tuple[bool, str]:
    with tempfile.NamedTemporaryFile("w", suffix=".txt", delete=False, encoding="utf-8") as f:
        script_path = f.name
        f.write("\n".join(script_lines))
        f.write("\n")

    try:
        result = run_hidden(["diskpart", "/s", script_path], timeout=None)
        output = (result.stdout or "") + "\n" + (result.stderr or "")
        return result.returncode == 0, output.strip()
    finally:
        try:
            os.remove(script_path)
        except OSError:
            pass


def build_diskpart_commands(
    disk_number: int,
    mode: str,
    fs_type: str = "NTFS",
    label: str = "FORMATADO",
) -> list[str]:
    safe_label = label.replace('"', "").strip()[:32] or "FORMATADO"

    commands = [
        f"select disk {disk_number}",
        "clean",
    ]

    if mode == "clean_only":
        return commands

    commands.extend([
        "convert gpt",
        "create partition primary",
        f'format fs={fs_type.lower()} quick label="{safe_label}"',
        "assign",
    ])
    return commands


class ConfirmDialog(tk.Toplevel):
    """Diálogo modal visual com opções claras de Sim / Não antes de executar."""

    def __init__(self, parent, mode_label: str, disks_info: list[dict]):
        super().__init__(parent)
        self.title("Confirmação de Operação — Limpador USB")
        self.geometry("560x450")
        self.minsize(520, 420)
        self.resizable(False, False)
        self.transient(parent)
        self.grab_set()

        self.confirmed = False

        container = ttk.Frame(self, padding=18)
        container.pack(fill=tk.BOTH, expand=True)

        header_frame = tk.Frame(container, bg="#c9302c", padding=10)
        header_frame.pack(fill=tk.X, pady=(0, 12))

        header_label = tk.Label(
            header_frame,
            text="⚠️ CONFIRMAÇÃO DE APAGAMENTO DE DISCO",
            font=("Segoe UI", 12, "bold"),
            fg="white",
            bg="#c9302c",
        )
        header_label.pack()

        action_box = ttk.LabelFrame(container, text="Ação a ser executada", padding=10)
        action_box.pack(fill=tk.X, pady=(0, 10))

        ttk.Label(
            action_box,
            text=mode_label,
            font=("Segoe UI", 10, "bold"),
            foreground="#1b4965",
        ).pack(anchor="w")

        disks_box = ttk.LabelFrame(container, text="Discos selecionados que serão totalmente formatados/limpos", padding=10)
        disks_box.pack(fill=tk.BOTH, expand=True, pady=(0, 10))

        text_frame = ttk.Frame(disks_box)
        text_frame.pack(fill=tk.BOTH, expand=True)

        scrollbar = ttk.Scrollbar(text_frame)
        scrollbar.pack(side=tk.RIGHT, fill=tk.Y)

        disk_list_text = tk.Text(
            text_frame,
            height=5,
            wrap="word",
            yscrollcommand=scrollbar.set,
            font=("Segoe UI", 9),
            bg="#fdfdfd",
            relief="solid",
            bd=1,
        )
        disk_list_text.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        scrollbar.config(command=disk_list_text.yview)

        for d in disks_info:
            num = d.get("Number")
            name = d.get("FriendlyName", "USB Drive")
            size = d.get("SizeGB", 0)
            letters = d.get("DriveLetters") or "Sem letra"
            used = d.get("UsedStr", "-")
            disk_list_text.insert(
                tk.END,
                f"• Disco {num}: {name}\n  Tamanho: {size} GB | Letra: {letters} | Espaço Usado: {used}\n\n",
            )

        disk_list_text.config(state=tk.DISABLED)

        warn_msg = ttk.Label(
            container,
            text="ATENÇÃO: TODOS OS DADOS NOS DISCOS SERÃO PERDIDOS PERMANENTEMENTE!",
            font=("Segoe UI", 9, "bold"),
            foreground="#a00000",
            anchor="center",
        )
        warn_msg.pack(fill=tk.X, pady=(0, 12))

        btn_frame = ttk.Frame(container)
        btn_frame.pack(fill=tk.X, pady=(4, 0))

        btn_no = ttk.Button(
            btn_frame,
            text="❌ Não, Cancelar",
            command=self.on_no,
            width=20,
        )
        btn_no.pack(side=tk.LEFT, padx=(0, 10))
        btn_no.focus_set()

        btn_yes = ttk.Button(
            btn_frame,
            text="✔ Sim, Iniciar Processo",
            command=self.on_yes,
            width=22,
        )
        btn_yes.pack(side=tk.RIGHT)

        self.protocol("WM_DELETE_WINDOW", self.on_no)
        self.center_on_parent(parent)

    def center_on_parent(self, parent):
        self.update_idletasks()
        pw = parent.winfo_width()
        ph = parent.winfo_height()
        px = parent.winfo_x()
        py = parent.winfo_y()

        w = self.winfo_width()
        h = self.winfo_height()

        x = px + (pw - w) // 2
        y = py + (ph - h) // 2
        self.geometry(f"+{x}+{y}")

    def on_yes(self):
        self.confirmed = True
        self.destroy()

    def on_no(self):
        self.confirmed = False
        self.destroy()


class DiskCleanerApp(tk.Tk):
    def __init__(self):
        super().__init__()

        self.title(APP_TITLE)
        self.geometry("1060x620")
        self.minsize(980, 560)

        self.disks: list[dict] = []
        self.refreshing = False
        self.executing = False
        self.stop_refresh = False

        self.mode_var = tk.StringVar(value="format_quick")
        self.fs_var = tk.StringVar(value="NTFS")
        self.label_var = tk.StringVar(value="FORMATADO")
        self.status_var = tk.StringVar(value="Iniciando...")
        self.last_signature = ""

        self.create_widgets()
        self.start_auto_refresh()

    def create_widgets(self):
        root = ttk.Frame(self, padding=14)
        root.pack(fill=tk.BOTH, expand=True)

        title = ttk.Label(
            root,
            text="Limpador / Formatador de HD e SSD conectados via USB",
            font=("Segoe UI", 16, "bold"),
        )
        title.pack(anchor="w")

        warning = ttk.Label(
            root,
            text="Atenção: esta ferramenta apaga permanentemente os discos selecionados. Use somente em HD/SSD externo via USB.",
            foreground="#9a0000",
            font=("Segoe UI", 10, "bold"),
        )
        warning.pack(anchor="w", pady=(4, 12))

        columns = (
            "select",
            "number",
            "name",
            "size",
            "used",
            "free",
            "letters",
            "style",
            "status",
            "health",
            "readonly",
        )
        self.tree = ttk.Treeview(root, columns=columns, show="headings", height=12)

        headings = {
            "select": "Selecionado",
            "number": "Disco",
            "name": "Nome / Modelo",
            "size": "Tamanho Total",
            "used": "Espaço Usado",
            "free": "Espaço Livre",
            "letters": "Letra",
            "style": "Partição",
            "status": "Status",
            "health": "Saúde",
            "readonly": "Somente leitura",
        }

        widths = {
            "select": 105,
            "number": 55,
            "name": 220,
            "size": 100,
            "used": 125,
            "free": 95,
            "letters": 65,
            "style": 75,
            "status": 75,
            "health": 75,
            "readonly": 105,
        }

        for col in columns:
            self.tree.heading(col, text=headings[col])
            self.tree.column(col, width=widths[col], anchor="center" if col not in ("name", "used", "free") else "w")

        self.tree.tag_configure("selected", background="#e8f4fc")
        self.tree.tag_configure("unselected", background="#ffffff")

        self.tree.pack(fill=tk.BOTH, expand=True)
        self.tree.bind("<ButtonRelease-1>", self.toggle_selection)
        self.tree.bind("<space>", self.toggle_keyboard_selection)

        options = ttk.LabelFrame(root, text="Ação", padding=12)
        options.pack(fill=tk.X, pady=(12, 8))

        ttk.Radiobutton(
            options,
            text="Limpar disco e deixar Não Alocado (equivalente ao clean do BAT)",
            variable=self.mode_var,
            value="clean_only",
        ).grid(row=0, column=0, sticky="w", padx=(0, 25), pady=3)

        ttk.Radiobutton(
            options,
            text="Formatação rápida: clean + criar partição + formatar + atribuir letra",
            variable=self.mode_var,
            value="format_quick",
        ).grid(row=1, column=0, sticky="w", padx=(0, 25), pady=3)

        ttk.Label(options, text="Sistema de arquivos:").grid(row=0, column=1, sticky="e", padx=(20, 5))
        fs_combo = ttk.Combobox(options, textvariable=self.fs_var, values=["NTFS", "exFAT", "FAT32"], state="readonly", width=10)
        fs_combo.grid(row=0, column=2, sticky="w")

        ttk.Label(options, text="Rótulo:").grid(row=1, column=1, sticky="e", padx=(20, 5))
        ttk.Entry(options, textvariable=self.label_var, width=18).grid(row=1, column=2, sticky="w")

        buttons = ttk.Frame(root)
        buttons.pack(fill=tk.X, pady=(6, 4))

        ttk.Button(buttons, text="Atualizar agora", command=self.refresh_disks).pack(side=tk.LEFT)
        ttk.Button(buttons, text="Selecionar todos", command=self.select_all).pack(side=tk.LEFT, padx=6)
        ttk.Button(buttons, text="Desmarcar todos", command=self.unselect_all).pack(side=tk.LEFT)

        self.action_btn = ttk.Button(
            buttons,
            text="Executar nos discos selecionados",
            command=self.confirm_and_execute,
        )
        self.action_btn.pack(side=tk.RIGHT)

        self.progress = ttk.Progressbar(root, mode="indeterminate")

        status = ttk.Label(root, textvariable=self.status_var)
        status.pack(anchor="w", pady=(4, 0))

    def disk_signature(self, disks: list[dict]) -> str:
        return "|".join(
            f"{d.get('Number')}:{d.get('FriendlyName')}:{d.get('Size')}:"
            f"{d.get('SizeRemaining')}:{d.get('DriveLetters')}:"
            f"{d.get('OperationalStatus')}:{d.get('HealthStatus')}:{d.get('IsReadOnly')}"
            for d in disks
        )

    def start_auto_refresh(self):
        self.refresh_disks()

        def loop():
            while not self.stop_refresh:
                time.sleep(REFRESH_SECONDS)
                self.refresh_disks(silent=True)

        threading.Thread(target=loop, daemon=True).start()

    def refresh_disks(self, silent: bool = False):
        if self.refreshing or self.executing:
            return

        self.refreshing = True

        def work():
            try:
                disks = get_usb_disks()
                signature = self.disk_signature(disks)

                def update_ui():
                    selected_numbers = set()
                    for item in self.tree.get_children():
                        vals = self.tree.item(item, "values")
                        if "☑" in vals[0] or vals[0].startswith("Sim"):
                            selected_numbers.add(int(vals[1]))

                    self.disks = disks
                    self.tree.delete(*self.tree.get_children())

                    for disk in disks:
                        number = int(disk.get("Number"))
                        selected = number in selected_numbers
                        select_str = "☑ Sim" if selected else "☐ Não"
                        tag = "selected" if selected else "unselected"

                        self.tree.insert(
                            "",
                            tk.END,
                            values=(
                                select_str,
                                number,
                                disk.get("FriendlyName", ""),
                                f"{disk.get('SizeGB', 0)} GB",
                                disk.get("UsedStr", "-"),
                                disk.get("FreeStr", "-"),
                                disk.get("DriveLetters") or "-",
                                disk.get("PartitionStyle", ""),
                                disk.get("OperationalStatus", ""),
                                disk.get("HealthStatus", ""),
                                "Sim" if disk.get("IsReadOnly") else "Não",
                            ),
                            tags=(tag,),
                        )

                    self.last_signature = signature
                    count = len(disks)
                    self.status_var.set(
                        f"{count} disco(s) USB detectado(s). Atualização automática a cada {REFRESH_SECONDS}s."
                    )

                if signature != self.last_signature or not silent:
                    self.after(0, update_ui)

            except Exception as exc:
                self.after(0, lambda: self.status_var.set(f"Erro ao listar discos: {exc}"))
            finally:
                self.refreshing = False

        threading.Thread(target=work, daemon=True).start()

    def toggle_selection(self, event):
        region = self.tree.identify("region", event.x, event.y)
        if region != "cell":
            return

        item = self.tree.identify_row(event.y)
        if not item:
            return

        values = list(self.tree.item(item, "values"))
        is_selected = "☑" in values[0] or values[0].startswith("Sim")
        new_selected = not is_selected

        values[0] = "☑ Sim" if new_selected else "☐ Não"
        tag = "selected" if new_selected else "unselected"
        self.tree.item(item, values=values, tags=(tag,))

    def toggle_keyboard_selection(self, event):
        selected_items = self.tree.selection()
        if not selected_items:
            return
        for item in selected_items:
            values = list(self.tree.item(item, "values"))
            is_selected = "☑" in values[0] or values[0].startswith("Sim")
            new_selected = not is_selected
            values[0] = "☑ Sim" if new_selected else "☐ Não"
            tag = "selected" if new_selected else "unselected"
            self.tree.item(item, values=values, tags=(tag,))

    def select_all(self):
        for item in self.tree.get_children():
            values = list(self.tree.item(item, "values"))
            values[0] = "☑ Sim"
            self.tree.item(item, values=values, tags=("selected",))

    def unselect_all(self):
        for item in self.tree.get_children():
            values = list(self.tree.item(item, "values"))
            values[0] = "☐ Não"
            self.tree.item(item, values=values, tags=("unselected",))

    def get_selected_disk_numbers(self) -> list[int]:
        selected = []
        for item in self.tree.get_children():
            values = self.tree.item(item, "values")
            if "☑" in values[0] or values[0].startswith("Sim"):
                selected.append(int(values[1]))
        return selected

    def confirm_and_execute(self):
        selected = self.get_selected_disk_numbers()

        if not selected:
            messagebox.showwarning("Nenhum disco selecionado", "Selecione pelo menos um disco USB.")
            return

        if self.mode_var.get() == "format_quick" and self.fs_var.get() == "FAT32":
            disks_by_number = {int(d.get("Number")): d for d in self.disks}
            large_disks = [
                n for n in selected
                if disks_by_number.get(n, {}).get("SizeGB", 0) > FAT32_MAX_GB
            ]
            if large_disks:
                nums = ", ".join(str(n) for n in large_disks)
                messagebox.showwarning(
                    "FAT32 não suportado",
                    f"FAT32 suporta no máximo {FAT32_MAX_GB} GB por partição.\n"
                    f"Disco(s) {nums} excede(m) esse limite. Use NTFS ou exFAT.",
                )
                return

        mode_label = (
            "LIMPAR E DEIXAR NÃO ALOCADO"
            if self.mode_var.get() == "clean_only"
            else f"FORMATAR RAPIDAMENTE EM {self.fs_var.get()} (Rótulo: {self.label_var.get().strip() or 'FORMATADO'})"
        )

        disks_info = [d for d in self.disks if int(d.get("Number")) in selected]

        dialog = ConfirmDialog(self, mode_label=mode_label, disks_info=disks_info)
        self.wait_window(dialog)

        if not dialog.confirmed:
            self.status_var.set("Operação cancelada pelo usuário.")
            return

        self.executing = True
        self.action_btn.config(state=tk.DISABLED)
        self.progress.pack(fill=tk.X, pady=(4, 4))
        self.progress.start(10)
        self.status_var.set("Executando operação... não remova os discos.")

        threading.Thread(target=self.execute_on_selected, args=(selected,), daemon=True).start()

    def execute_on_selected(self, selected: list[int]):
        logs = []

        try:
            validated, error = validate_selected_disks(selected)
            if error:
                raise RuntimeError(error)

            for disk in validated:
                disk_number = int(disk.get("Number"))
                self.after(0, lambda n=disk_number: self.status_var.set(f"Processando Disco {n}..."))

                commands = build_diskpart_commands(
                    disk_number=disk_number,
                    mode=self.mode_var.get(),
                    fs_type=self.fs_var.get(),
                    label=self.label_var.get().strip() or "FORMATADO",
                )

                ok, output = diskpart(commands)
                logs.append(f"===== Disco {disk_number} =====\n{output}\n")

                if not ok:
                    raise RuntimeError(f"Falha ao processar o Disco {disk_number}.")

            self.after(0, lambda: messagebox.showinfo("Concluído", "Operação finalizada com sucesso nos discos selecionados."))
            self.after(0, self.refresh_disks)

        except Exception as exc:
            full_log = "\n".join(logs) + f"\n\nErro: {exc}"
            log_path = save_error_log(full_log)
            self.after(
                0,
                lambda: messagebox.showerror(
                    "Erro",
                    f"{exc}\n\nLog completo salvo em:\n{log_path}",
                ),
            )
        finally:
            self.executing = False
            self.after(0, lambda: self.progress.stop())
            self.after(0, lambda: self.progress.pack_forget())
            self.after(0, lambda: self.action_btn.config(state=tk.NORMAL))
            self.after(0, lambda: self.status_var.set("Pronto."))

    def destroy(self):
        self.stop_refresh = True
        super().destroy()


if __name__ == "__main__":
    relaunch_as_admin()
    app = DiskCleanerApp()
    app.mainloop()
admin()
    app = DiskCleanerApp()
    app.mainloop()