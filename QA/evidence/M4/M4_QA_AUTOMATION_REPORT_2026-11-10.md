# M3 QA 자동화 보고서

- 생성 시각: 2026-11-10 23:07:36
- 종합: PASS

## 다중 Seed 생성 검증 — PASS

검증 범위: Run Seed 20261106~20262105
총 층: 5000
실패: 0
Loop/Tree: 3991/1009
실패 목록: C:/Users/user/AppData/LocalLow/DefaultCompany/202202825 문지성 PROJECT0\M3Measurements\seed_validation_failures.csv

## 아이템 드롭 분포 — PASS

F1: N 78.96% / R 17.95% / E 2.11% / U 0.99% / L 0% — PASS
F2: N 69.21% / R 24.8% / E 4.99% / U 1% / L 0% — PASS
F3: N 59.13% / R 29.95% / E 9.98% / U 0.95% / L 0% — PASS
F4: N 58.98% / R 30.08% / E 10% / U 0.94% / L 0% — PASS
아이템 정의: 13개, ID 중복 없음 True
분포 CSV: C:/Users/user/AppData/LocalLow/DefaultCompany/202202825 문지성 PROJECT0\M3Measurements\loot_distribution.csv

## 보스 드롭 분포 — PASS

원정 100,000회, 생성 아이템 110,176개, 아이템 판정 실패 0
수량 0/1/2: 9.86% / 70.11% / 20.03% — PASS
등급 Rare/Epic/Unique: 49.91% / 40.01% / 10.08% — PASS
분포 CSV: C:/Users/user/AppData/LocalLow/DefaultCompany/202202825 문지성 PROJECT0\M3Measurements\boss_loot_distribution.csv

## 핵심 회귀 — PASS

PASS — 이동 1Tick
PASS — 공격 5Tick
PASS — 줍기 1Tick
PASS — 버리기 1Tick
PASS — 장비 교체 2Tick
PASS — 기본 가방 6칸
PASS — M4 원정 1~5층
PASS — 맵 Seed 결정성 100회
PASS — 드롭 Seed 결정성·Bag 드롭 제외 100회

## 원정 CSV 분석 — PASS

파일: C:/Users/user/AppData/LocalLow/DefaultCompany/202202825 문지성 PROJECT0\M3Measurements\expedition_runs_v3.csv
전체 4, Random 표본 4, 생환 2, 사망 2, MIA 0
Tick 분석은 Random 모드만 포함하며 Fixed 반복 표본은 제외했다.
3층 생환: 0건, 평균 0Tick. 조기 생환 표본으로 별도 집계한다.
5층 생환: 2건, 평균 654Tick. M4 측정 기준이며 목표 범위는 미확정이다.
평균 이동/대기/공격/루팅: 295/11.5/41.25/25.5
평균 조우/처치/받은 피해/회복: 2/1.5/68.5/17.5
평균 상자/획득/출구 경로/출구 방 Enemy: 5/5.5/207.5/1.5
긴 원정 TOP 5: Seed -2070528383 676Tick/Escaped, Seed 936516636 632Tick/Escaped, Seed -1438665259 211Tick/Dead, Seed -1435048743 89Tick/Dead


