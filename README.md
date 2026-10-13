# F1 Race Simulator

Курсовий проєкт з об'єктно-орієнтованого програмування: симуляція гонки Формули 1 сезону 2026 року мовою C# (.NET 8, Windows Forms).

Програма проводить кваліфікацію і гонку (від старту з решітки на першому колі до клетчастого прапора) на будь-якій із 23 трас календаря 2026 року з усіма 22 пілотами 11 команд. Боліди рухаються справжньою схемою траси (північ — угорі, як на карті), шини зношуються, пілоти заїжджають на піт-стопи за своєю стратегією, таблиця позицій оновлюється після кожного обгону, а результати потрапляють у таблицю чемпіонату.

Для обраного пілота показується телеметрія боліда: знос і температура шин (з робочим вікном складу), температура і знос двигуна, залишок палива в кілограмах і на скільки кіл його вистачить, заряд батареї ERS та режим їзди (рівний темп, атака, захист, економія).

## Що потрібно

- Windows 10 або 11.
- Visual Studio 2022 (версія 17.8 або новіша) з робочим навантаженням **«.NET desktop development»** («Розробка класичних застосунків .NET»).
- .NET 8 SDK (встановлюється разом із Visual Studio).

## Як запустити

1. Склонуйте репозиторій або завантажте ZIP (кнопка **Code → Download ZIP**).
2. Відкрийте файл `F1RaceSimulator.sln` у Visual Studio.
3. Переконайтеся, що стартовий проєкт — **F1RaceSimulator.WinForms** (правою кнопкою по проєкту → *Set as Startup Project*).
4. Натисніть **F5**.

Під час першої збірки Visual Studio сама завантажить пакети NuGet для тестів (потрібен інтернет).

## Як запустити тести

**Test → Run All Tests** (або `Ctrl+R, A`). Вікно результатів — **Test → Test Explorer**.
З командного рядка: `dotnet test`.

## Керування

| Дія | Як |
| --- | --- |
| Старт / пауза | кнопка «Старт» або **F5** |
| Почати гонку знову (нова кваліфікація) | «Почати знову» або **Ctrl+R** |
| Обрати етап | список «Етап» на панелі інструментів |
| Змінити швидкість | список «Швидкість»: ×10, ×25, ×50, ×100 |
| Обрати пілота | клацнути рядок у таблиці або болід на трасі |
| Таблиця чемпіонату | меню «Сезон» або **F9** |
| Зберегти / відкрити чемпіонат | меню «Файл», **Ctrl+S** / **Ctrl+O** |

## Структура рішення

```
F1RaceSimulator.sln
├── src/
│   ├── F1RaceSimulator.Core/        логіка симуляції, без інтерфейсу
│   │   ├── Geometry/                Vec2, Polyline — геометрія схеми траси
│   │   ├── Models/                  Team, Driver, Track, TrackLayout, TyreCompound
│   │   ├── Components/              CarComponent (абстрактний) → PowerUnit, FuelTank, ErsBattery
│   │   ├── Tyres/                   Tyre (← CarComponent) → SoftTyre, MediumTyre, HardTyre; TyreFactory
│   │   ├── Strategies/              IPitStrategy → ThresholdPitStrategy → AggressivePitStrategy
│   │   ├── Simulation/              Car, Race, DrivingMode, Qualifying, IRaceObserver, EventLogObserver, ...
│   │   └── Season/                  SeasonData, DataLoader, CarFactory, Championship
│   └── F1RaceSimulator.WinForms/    вікна програми
│       ├── MainForm                 головне вікно
│       ├── ChampionshipForm         таблиця чемпіонату
│       ├── TrackPanel               панель зі схемою траси
│       ├── TrackRenderer            малювання траси і болідів (GDI+)
│       └── TelemetryPanel           телеметрія обраного боліда (шкали шин, двигуна, палива, ERS)
├── tests/F1RaceSimulator.Tests/     модульні тести xUnit
├── data/                            teams.json, drivers.json, tracks.json
└── docs/uml/                        UML-діаграми (PlantUML + PNG)
```

## Принципи ООП і патерни в коді

| Що | Де |
| --- | --- |
| Інкапсуляція | `Car`: стан змінюється лише методами `Advance`, `EnterPitLane`, `ChangeTyres`; температура і паливо — лише через `Update` компонента |
| Абстракція | абстрактні класи `CarComponent`, `Tyre`; інтерфейси `IPitStrategy`, `IRaceObserver`, `IRandomSource` |
| Наслідування | `Tyre`, `PowerUnit`, `FuelTank`, `ErsBattery` ← `CarComponent`; `SoftTyre`, `MediumTyre`, `HardTyre` ← `Tyre`; `AggressivePitStrategy` ← `ThresholdPitStrategy` |
| Поліморфізм | `CarComponent.Update` (кожен компонент оновлюється по-своєму), `Tyre.PaceMultiplier` (перевизначено в `SoftTyre`), `IPitStrategy.ShouldPit` |
| Strategy | `IPitStrategy` і дві стратегії піт-стопів |
| Observer | `Race` → `IRaceObserver` (`EventLogObserver`, `MainForm`) |
| Factory | `TyreFactory`, `PitStrategyFactory`, `CarFactory` |
| Singleton | `Championship.Instance` |

## Дані

Усі дані сезону лежать у папці `data/` у форматі JSON і копіюються поруч із програмою під час збірки. Їх можна змінювати без перекомпіляції коду.

- `teams.json` — 11 команд: назва, колір, умовний рейтинг боліда.
- `drivers.json` — 22 пілоти: команда, умовні рейтинги швидкості, стабільності й дбайливості до шин, стратегія піт-стопів.
- `tracks.json` — 23 етапи календаря 2026: довжина кола, кількість кіл, типовий час кола, абразивність, погода, температура повітря і схема траси.

Склади команд — за даними [Formula1.com](https://www.formula1.com/en/latest/article/2026-line-ups-confirmed-in-full-who-is-on-the-grid-for-next-season.3TyLfOUjOwpBKOp3KX1PiS). Рейтинги пілотів і болідів, а також числа моделі температур, палива та ERS умовні — це навчальна модель, а не точна фізика. Схеми трас побудовані за GPS-координатами з набору даних [f1-circuits](https://github.com/bacinger/f1-circuits); піт-лейни намальовані схематично.

## Примітка про версію .NET

Проєкт націлений на .NET 8 (LTS), який працює у Visual Studio 2022. Підтримка .NET 8 від Microsoft триває до 10 листопада 2026 року. Щоб перейти на .NET 10, змініть `net8.0` на `net10.0` (і `net8.0-windows` на `net10.0-windows`) у трьох файлах `.csproj` і відкрийте рішення у Visual Studio 2026.
