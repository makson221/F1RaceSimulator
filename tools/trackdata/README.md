# Генератор схем трас

Скрипти, якими згенеровано `layout` і `profile` у `data/tracks.json`: контур траси в офіційній
орієнтації F1, номери поворотів, межі трьох секторів і профіль швидкості / передачі / обертів /
газу / гальма вздовж кола. Програмі ці скрипти не потрібні — вони лише для оновлення даних.

Джерело — офіційні дані хронометражу F1, які публікує [TracingInsights](https://github.com/TracingInsights)
(отримані через бібліотеку [FastF1](https://github.com/theOehrly/Fast-F1), ліцензія Apache-2.0).
Орієнтацію карти беремо з поля `Rotation` (той самий кут, яким FastF1 і трансляція F1 повертають трасу).

## Як запустити (Python 3.10+, git)

```bash
mkdir dl && cd dl
# лише потрібні файли, без історії
git clone --filter=blob:none --no-checkout https://github.com/TracingInsights/2026 ti2026
git clone --filter=blob:none --no-checkout https://github.com/TracingInsights/2025 ti2025
# 1) спершу таблиці кіл і повороти
for r in ti2026 ti2025; do
  git -C $r sparse-checkout set --no-cone "*/session_laptimes.json" "*/corners.json" "*/cor.json"
  git -C $r checkout
done
cd ..
python3 pick.py dl            # обирає найкращі кола → picked.json
# 2) докачати телеметрію лише обраних кіл (шляхи є в picked.json), напр.:
#    git -C dl/ti2026 sparse-checkout add "Italian Grand Prix/Race/NOR/53_tel.json"
python3 gen_official.py dl ../../data/tracks.json picked.json
python3 fmt.py ../../data/tracks.json ../../data/tracks.json   # компактний запис масивів
```

- `events.py` — відповідність id траси в програмі події в наборі даних (траси, яких ще не було у 2026 до
  листопада, — з 2025 року).
- Для Мадрида (`es-2026`) і Сепанга (`my-1999`) офіційних даних про повороти немає, тому номери
  поворотів знаходяться за кривиною траси (`AUTO_TURNS`), а карта орієнтована на північ.
- `typicalLapSeconds` = найкраще коло гонки × 1.015; `pitLaneSeconds` — з `PitLossNormal`.
