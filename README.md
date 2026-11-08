<div align="center">

<img src="Assets/app.ico" alt="TrayFolderFlyout Logo" width="112" height="112" />

# 📁 TrayFolderFlyout

### Нативные всплывающие папки для панели задач Windows 11
**Fluent Design • Акриловый блюр (Acrylic) • Мгновенный отклик ~28 мс (NativeAOT) • Чистый Рабочий стол**

[![Build and Release](https://github.com/INTI1337/TrayFolderFlyout/actions/workflows/release.yml/badge.svg)](https://github.com/INTI1337/TrayFolderFlyout/actions/workflows/release.yml)
[![Latest Release](https://img.shields.io/github/v/release/INTI1337/TrayFolderFlyout?style=for-the-badge&color=6366f1&logo=github)](https://github.com/INTI1337/TrayFolderFlyout/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/INTI1337/TrayFolderFlyout/total?style=for-the-badge&color=10b981&logo=windows)](https://github.com/INTI1337/TrayFolderFlyout/releases)
[![Stars](https://img.shields.io/github/stars/INTI1337/TrayFolderFlyout?style=for-the-badge&color=f59e0b&logo=reverbnation&logoColor=white)](https://github.com/INTI1337/TrayFolderFlyout/stargazers)
[![Platform](https://img.shields.io/badge/Platform-Windows%2011%20%7C%20x64-0078D4?style=for-the-badge&logo=windows&logoColor=white)](https://github.com/INTI1337/TrayFolderFlyout)
[![License](https://img.shields.io/github/license/INTI1337/TrayFolderFlyout?style=for-the-badge&color=ec4899)](LICENSE)

<br/>

### **[📥 Скачать последнюю версию (Setup / Portable)](https://github.com/INTI1337/TrayFolderFlyout/releases/latest)**

<p align="center">
  <a href="#-таблица-преимуществ"><b>Преимущества</b></a> •
  <a href="#-как-скачать-и-установить"><b>Установка</b></a> •
  <a href="#-основные-возможности"><b>Возможности</b></a> •
  <a href="#-безопасность-и-прозрачность-сборки"><b>Безопасность</b></a> •
  <a href="#-сборка-из-исходного-кода"><b>Сборка</b></a> •
  <a href="#-благодарности--contributors"><b>Благодарности</b></a> •
  <a href="#-english-overview"><b>English Overview 🌐</b></a>
</p>

</div>

---

<!-- Вставьте сюда скриншоты через Ctrl+V на GitHub -->
<div align="center">

  <img width="562" height="299" alt="Анимация" src="https://github.com/user-attachments/assets/67bf3559-f690-4f36-b02d-09dbd364f7fa" />

<br/><br/>

<img width="1182" height="784" alt="Анимация 2" src="https://github.com/user-attachments/assets/317c2345-94a3-49fb-a104-0063c6ce034a" />

<p><i>Компактное всплывающее меню папки на панели задач и интуитивный менеджер настроек в нативном стиле Windows 11 Fluent Dark</i></p>

</div>

---

## 💡 Зачем нужен TrayFolderFlyout?

В Windows 11 компания Microsoft удалила классические панели инструментов (Toolbars), из-за чего пользователи потеряли возможность закреплять папки с быстрыми ярлыками на панели задач. В результате Рабочий стол захламляется десятками ярлыков, а поиск нужных утилит через меню «Пуск» отнимает лишнее время.

**TrayFolderFlyout** решает эту проблему современным и элегантным путём:
Каждая папка закрепляется на панели задач Windows 11 как самостоятельная аккуратная иконка. При клике на неё мгновенно всплывает компактное меню (**Flyout**) прямо над значком — визуально и по поведению в точности повторяющее нативное всплывающее меню скрытых значков трея Windows 11.

---

## ⚡ Таблица преимуществ

| Возможность | **TrayFolderFlyout** 📁 | Стандартный Рабочий стол 🪟 | Обычные ярлыки панели задач 📌 |
| :--- | :---: | :---: | :---: |
| **Скорость открытия** | **⚡ ~28 мс (NativeAOT)** *(~12 мс передача IPC)* | ⏳ 1–2 сек *(тяжёлый запуск Проводника)* | ❌ Меню отсутствует |
| **Интеграция со стилем Windows 11** | **✨ Нативный Fluent Acrylic & Mica** | 📁 Стандартное белое окно папки | ❌ Нет всплывающих меню |
| **Кастомные иконки в панели задач** | **🎨 10 градиентных сквирклов + любые .ico/.png/.exe** | ❌ Стандартная желтая папка | ⚠️ Только оригинальная иконка программы |
| **«Безымянные папки» (Clean Look)** | **🫥 Да (символ Брайля, чистая иконка без подписи)** | ❌ Невозможно без хаков | ❌ Всегда отображается имя файла |
| **Любое положение панели задач** | **🧭 Снизу, сверху, слева, справа (авто-привязка)** | ❌ — | ❌ — |
| **Сворачиваемый заголовок (Collapsible)** | **🖼️ Да (выезжает по hover 10px, убирается по Esc)** | ❌ Нет | ❌ — |
| **Автономность ярлыков** | **📦 Да (копирование в `SavedLinks`)** | ❌ Нет *(удалил оригинал — ярлык сломался)* | ❌ Нет |
| **Интеллектуальное автовосстановление** | **🩹 Да (`Auto-Heal` при перемещении файлов)** | ❌ Нет | ❌ Нет |
| **Smart Portable режим** | **💼 Да (хранение данных в `UserData`, 0 следов в системе)** | ❌ — | ❌ — |
| **Резервное копирование (JSON)** | **💾 Да (Экспорт / Импорт настроек в 1 клик)** | ❌ Нет | ❌ Нет |
| **Нагрузка на систему** | **🍃 0% CPU, минимум RAM** | — | — |
| **Чистота рабочего пространства** | **✨ 100% чистый Рабочий стол без мусора** | ❌ Захламлён сотнями значков | ⚠️ Ограниченное число ярлыков в панели |

---

## 📥 Как скачать и установить

> [!TIP]
> Приложение скомпилировано в формате **Self-Contained Single File**: вам **не нужно** вручную устанавливать .NET Runtime или сторонние библиотеки. Всё уже встроено в бинарник и запускается на любой Windows 10/11 x64.

### Вариант 1. Автоматический установщик (Рекомендуется)
1. Перейдите на страницу **[Последнего релиза](https://github.com/INTI1337/TrayFolderFlyout/releases/latest)**.
2. Скачайте **`TrayFolderFlyout_Setup.exe`**.
3. Запустите установщик и нажмите **«Установить»**:
   - Приложение и NativeAOT-лаунчер установятся в папку `%LocalAppData%\Programs\TrayFolderFlyout\`.
   - Будут созданы удобные ярлыки и запись в системном меню «Установленные приложения».
   - Фоновая служба автоматически запустится и пропишется в автозагрузку.

### Вариант 2. Портативная версия (Portable)
1. В разделе **[Releases](https://github.com/INTI1337/TrayFolderFlyout/releases/latest)** скачайте **`TrayFolderFlyout_v1.2.0_Portable_win64.zip`** (или `TrayFolderFlyout_Portable.exe`).
2. Распакуйте архив в любую папку и запустите. Программа готова к работе без инсталляции.
3. **Умный режим без следов**: если рядом с `.exe` находится папка `UserData`, файл `config.json` или маркер `.portable`, все настройки, иконки и ярлыки хранятся строго локально, не затрагивая системный реестр и `%AppData%`.

---

> [!IMPORTANT]
> ### 💡 Примечание при первом запуске (Windows SmartScreen)
> **TrayFolderFlyout** — это 100% открытый, бесплатный и прозрачный проект (Open Source).
> 
> У независимых Open-Source разработчиков нет корпоративного сертификата *Microsoft EV Code Signing* (стоимостью от $500–1000 ежегодно), поэтому встроенный защитный экран **Windows SmartScreen** при первом запуске нового `.exe` может отобразить стандартное синее окно: *«Система Windows защитила ваш компьютер»*.
> 
> **Как продолжить запуск:**
> 1. В синем окне SmartScreen нажмите ссылку **«Подробнее»** (*More info*).
> 2. Нажмите появившуюся кнопку **«Выполнить в любом случае»** (*Run anyway*).
> 
> 🛡️ *Исходный код приложения полностью открыт для проверки прямо в этом репозитории. Программа не собирает персональные данные и не совершает скрытых сетевых запросов.*

---

## 🛡️ Безопасность и прозрачность сборки

1. **100% чистая автоматическая сборка (GitHub Actions CI/CD)**:
   Весь код открыт, а релизные бинарные файлы (`TrayFolderFlyout_Setup.exe`, `TrayFolderFlyout_Portable.exe`, `TrayFolderFlyout_v1.2.0_Portable_win64.zip`) компилируются автоматически на изолированных облачных серверах GitHub Actions напрямую из публичных коммитов репозитория. В релиз не попадает ни одного файла, собранного на локальном компьютере автора.
2. **Криптографическая аттестация подлинности (GitHub Artifact Attestations)**:
   Каждый исполняемый файл подписывается официальной службой аттестации GitHub (Sigstore). Вы можете в любой момент удостовериться, что скачанный файл был собран именно из исходного кода этого репозитория:
   ```bash
   gh attestation verify TrayFolderFlyout_Portable.exe --owner INTI1337
   ```
3. **Отчёты VirusTotal и контрольные суммы SHA256**:
   В описании каждого релиза автоматически публикуются прямые ссылки на результаты проверки через **VirusTotal API** и хэши `checksums_sha256.txt` для проверки целостности файлов.

---

## ✨ Основные возможности

### ⚡ 1. Ультрабыстрый отклик (NativeAOT Launcher)
- **Ультралёгкий NativeAOT лаунчер (`TrayFolderFlyout.Launcher.exe`, ~1.9 МБ)**: передача IPC-сообщения по именованному пайпу (`TrayFolderFlyout_Pipe`) всего за **~12 мс**, отклик появления окна **~28 мс** (сжатие задержки в 16 раз!) без холодного старта тяжёлого WPF-процесса (~450 мс).
- **Архитектура Single-Instance Daemon**: приложение непрерывно работает в фоне как легковесный фоновый процесс (0% CPU, минимум памяти). Если служба не запущена, лаунчер прозрачно передаёт запуск основному процессу.

### 🖼️ 2. Сворачиваемый заголовок (Collapsible Header)
- Плашка папки не занимает лишнее место на экране и прячется в компактный оверлей.
- Заголовок плавно выезжает при наведении курсора на верхнюю кромку (10px) и мгновенно убирается по нажатию клавиши `Esc`.

### 📐 3. Пресеты значков с автоподгонкой
- Автоматическое масштабирование иконок под высоту и расположение панели задач на текущем мониторе.
- Адаптивная плотность сетки для комфортного отображения любого количества ярлыков.

### 📌 4. Поддержка системных ярлыков
- Возможность добавлять в папки любые закреплённые ярлыки прямо из панели задач Windows 11 и системного меню «Пуск».
- Автоматическое разрешение путей к целевым исполняемым файлам и приложениям.

### 🎯 5. Pixel-perfect HiDPI и точное позиционирование
- Прижатие флайаута вплотную к панели задач с рамкой толщиной ровно в 1 физический пиксель без артефактов и смазывания на системных масштабах 125%, 150% и 200%.
- Автоматическое определение любого положения панели задач (снизу, сверху, слева или справа) с соблюдением рабочих границ экрана.

### 🎨 6. Кастомные иконки и 10 сочных градиентных пресетов
- 10 встроенных дизайнерских сквирклов в стилистике Fluent Design: «Игры», «Работа», «Система», «Медиа», «Код», «Дизайн», «Интернет», «Офис», «Безопасность», «Папка».
- Возможность установить собственную картинку или иконку любого формата (`.ico`, `.png`, `.jpg`, `.jpeg`, `.bmp`, `.exe`) — они на лету конвертируются в многослойный `.ico` со всеми системными разрешениями (16x16, 32x32, 48x48, 256x256).

### 🫥 7. «Безымянные папки» для ультрачистого вида панели задач
- При пустом названии папки программа автоматически подставляет невидимый символ Брайля (`\u2800`). На панели задач закрепляется исключительно красивая иконка без лишних подписей, точек и многоточий.

### 📦 8. Автономное хранилище ярлыков (`SavedLinks`)
- При перетаскивании (Drag-and-Drop) файлов и ярлыков в окно менеджера программа автоматически сохраняет копию `.lnk` / `.url` внутри папки `SavedLinks`.
- **Вы можете смело удалять ярлыки с Рабочего стола** — папка продолжит стабильно запускать все ваши программы.

### 🩹 9. Интеллектуальное автовосстановление (Auto-Heal)
- Если целевые файлы были перемещены во вложенные подкаталоги или меню «Пуск», встроенный интеллектуальный сканер автоматически найдет их новое расположение и обновит пути.

### 💾 10. Экспорт и импорт настроек (Резервное копирование в JSON)
- Экспорт всей структуры папок, порядка элементов и настроек в читаемый `.json` файл в один клик.
- Импорт с мгновенным обновлением интерфейса без необходимости перезапускать программу.

---

## 🗑️ Удаление

Приложение удаляется стандартно и начисто:
- **Параметры Windows** → **Приложения** → **Установленные приложения** → **TrayFolderFlyout** → **Удалить**.
- Либо запуском `Uninstall.exe` в каталоге программы.

Деинсталлятор корректно завершает фоновый процесс, удаляет созданные ярлыки и очищает автозагрузку.

---

## 🛠️ Сборка из исходного кода

### Требования
- ОС: Windows 10 (1809+) или Windows 11 (x64)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) / [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- Для сборки NativeAOT: Visual Studio с компонентом *«Разработка классических приложений на C++»* (Desktop Development with C++)

### Команды сборки:

1. **Клонируйте репозиторий:**
   ```bash
   git clone https://github.com/INTI1337/TrayFolderFlyout.git
   cd TrayFolderFlyout
   ```

2. **Сборка NativeAOT-лаунчера (`TrayFolderFlyout.Launcher`):**
   ```bash
   dotnet publish TrayFolderFlyout.Launcher/TrayFolderFlyout.Launcher.csproj -c Release -r win-x64 -o ./Publish/App
   ```

3. **Сборка основного автономного приложения (Portable Single-File):**
   ```bash
   dotnet publish TrayFolderFlyout.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:DebugSymbols=false -o ./Publish/App
   ```

4. **Сборка установщика `TrayFolderFlyout_Setup.exe`:**
   ```bash
   dotnet publish TrayFolderFlyout.Setup/TrayFolderFlyout.Setup.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o ./Release
   ```

---

## 🏛️ Архитектура проекта

```
TrayFolderFlyout/
├── .github/
│   ├── workflows/release.yml      # CI/CD автоматическая сборка, аттестация и релизы
│   └── ISSUE_TEMPLATE/            # Шаблоны баг-репортов и предложений
├── Assets/                        # Иконка приложения (256x256 Bento grid) и стили
├── Models/                        # Доменные модели (AppConfig, FolderGroup, FolderItem)
├── Native/                        # Win32 P/Invoke, COM IShellLinkW, IShellItemImageFactory
├── Services/                      # Сервисы IpcService, ConfigService, IconService, TrayService
├── Styles/                        # Стили Fluent Dark, шаблоны элементов управления
├── ViewModels/                    # MVVM связка для реактивного интерфейса
├── Views/                         # FlyoutWindow (попап), IconPickerWindow, InputDialog
├── TrayFolderFlyout.Launcher/     # Сверхбыстрый NativeAOT лаунчер для закреплённых ярлыков (~1.9 МБ)
│   ├── Program.cs                 # Отправка IPC за ~12 мс и запуск хоста
│   └── TrayFolderFlyout.Launcher.csproj
├── TrayFolderFlyout.Setup/        # Отдельный легковесный установщик с Fluent UI
│   ├── MainWindow.xaml            # Окно инсталлятора
│   ├── InstallerLogic.cs          # Распаковка, реестр, автозагрузка, деинсталлятор
│   └── ShortcutHelper.cs          # Создание ярлыков Windows Shell
├── App.xaml / App.xaml.cs         # Точка входа, Single Instance Mutex, диспетчер IPC
├── MainWindow.xaml                # Менеджер папок и визуальный редактор
└── TrayFolderFlyout.csproj        # Спецификация сборки .NET 8 WPF
```

---

## 🤝 Благодарности / Contributors

Огромный респект [@hardway777](https://github.com/hardway777) за разработку легковесного NativeAOT-лаунчера (`TrayFolderFlyout.Launcher`), сжатие отклика клика в 16 раз (с ~456 мс до ~28 мс), поддержку закреплённых ярлыков и глубокую полировку оверлея под HiDPI-дисплеи.

---

<br/>

## 🌐 English Overview

### 📁 Custom Fluent Flyout Folders for Windows 11 Taskbar

**TrayFolderFlyout** is a lightweight, native C# (.NET 8 WPF) Windows 11 application that brings back folder toolbars to your Taskbar with a modern, sleek Fluent Acrylic interface.

### 🌟 Key Features
- **⚡ Ultra-fast NativeAOT Launcher**: Dedicated NativeAOT binary (`TrayFolderFlyout.Launcher.exe`, ~1.9 MB) sends an IPC message via a named pipe in ~12 ms, slashing click-to-visible latency by 16x (from ~456 ms down to ~28 ms) without waiting for a cold WPF process boot.
- **🖼️ Collapsible Header**: Clean, unobtrusive header overlays the folder when hovering over the top 10px edge and instantly tucks away with Esc.
- **📐 Auto-Fitting Icon Presets**: Dynamic icon scaling that automatically adapts to the height and placement of the taskbar on the active monitor.
- **📌 System & Pinned Shortcut Support**: Seamlessly organize and launch pinned shortcuts from the Windows 11 taskbar and Start Menu.
- **🎯 Pixel-Perfect HiDPI**: Crisp 1-physical-pixel border alignment flush against the taskbar without scaling blur on 125%, 150%, or 200% displays.
- **🧭 Multi-Orientation Taskbar Support**: Supports bottom, top, left, and right taskbar layouts with automatic 12px margin bounds clamping.
- **🎨 Custom Squircle Presets & Icons**: Includes 10 colorful Fluent Design gradient squircle icons, or assign any custom `.ico`, `.png`, `.jpg`, or `.exe` file.
- **🫥 Nameless Taskbar Folders**: Leave the folder name blank to create a clean, icon-only shortcut on your taskbar using an invisible Braille blank character (`\u2800`).
- **💼 Smart Portable Mode**: Zero footprint on the host system when `UserData/`, `config.json`, or `.portable` marker is present next to the executable.
- **📦 Autonomous Shortcuts (`SavedLinks`)**: Dropping shortcuts copies them locally, so you can safely clean up and delete desktop clutter without breaking launches.
- **💾 JSON Backup & Restore**: Export and import your entire folder structure in one click with instant hot-reload.
- **🩹 Auto-Healing**: Automatically scans and restores moved applications and shortcuts.
- **🪟 Windows 11 Fluent Aesthetics**: Native Acrylic blur, smooth rounded corners, and Dark mode matching the Windows 11 system flyouts.

### 🛡️ Security & Build Verification
1. **Automated CI/CD**: All release executables are built strictly on clean GitHub Actions runners directly from public commits.
2. **Cryptographic Attestations**: Verify release artifact provenance using GitHub CLI:
   ```bash
   gh attestation verify TrayFolderFlyout_Portable.exe --owner INTI1337
   ```
3. **VirusTotal & Checksums**: Every release includes automated VirusTotal report links and SHA256 checksums (`checksums_sha256.txt`).

### 📥 Download & Install
1. Download **`TrayFolderFlyout_Setup.exe`** or **`TrayFolderFlyout_v1.2.0_Portable_win64.zip`** from the **[Latest Release](https://github.com/INTI1337/TrayFolderFlyout/releases/latest)**.
2. Run the installer or unpack the portable archive.
3. If Windows SmartScreen appears (*"Windows protected your PC"*), click **"More info"** and then **"Run anyway"** (the app is completely open source and does not carry an expensive corporate EV certificate).

---

## 🤝 Special Thanks / Contributors

Huge thanks to [@hardway777](https://github.com/hardway777) for engineering the ultra-fast NativeAOT launcher (`TrayFolderFlyout.Launcher`), reducing click latency by 16x (from ~456 ms down to ~28 ms), implementing pinned taskbar shortcut integration, and fine-tuning the flyout overlay for HiDPI screens.

---

## 📄 Лицензия / License

Проект распространяется под открытой лицензией [MIT License](LICENSE). Вы можете свободно использовать, модифицировать и распространять данный код.  
*Distributed under the MIT License. See [LICENSE](LICENSE) for more information.*
