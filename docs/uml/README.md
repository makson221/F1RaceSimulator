# UML-діаграми

Вихідні файли — у форматі [PlantUML](https://plantuml.com) (`*.puml`), поруч лежать готові зображення PNG для пояснювальної записки.

| Файл | Діаграма |
| --- | --- |
| `01-use-cases` | Варіанти використання |
| `02-components` | Компоненти (структура рішення) |
| `03-class-model` | Класи: модель, шини, стратегії |
| `04-class-simulation` | Класи: гонка, Observer, Singleton |
| `05-class-ui` | Класи: інтерфейс Windows Forms |
| `06-sequence-race-step` | Послідовність: крок симуляції з піт-стопом |
| `07-state-car` | Стани боліда |
| `08-class-components` | Класи: компоненти боліда і телеметрія (шини, двигун, паливо, ERS) |

Щоб перегенерувати зображення: `plantuml -charset UTF-8 -tpng *.puml` (або розширення PlantUML для VS Code / онлайн-редактор plantuml.com).
