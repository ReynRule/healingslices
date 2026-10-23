# Healing Slices 🍉🔪
Perfect Slices 스타일의 3D 힐링 과일 썰기 게임 (Unity 2022.3, Android).
화면을 터치하면 그 위치에서 칼이 내려와 과일을 썹니다. 정중앙을 썰면 Perfect 보너스!

## 포함 기능
- 나무 선로 위를 지나가는 9종 과일 (사과·오렌지·레몬·수박·키위·복숭아·자두·라임·용과)
- 5종 칼 (Chef Knife / Cleaver / Golden Blade / Katana / Ice Blade) — 우상단 버튼으로 변경
- 썰리는 소리·휘두르는 소리·Perfect 차임·잔잔한 BGM (모두 코드로 합성)
- 단면(과육+껍질)이 보이는 두 조각, 과일 파편, 과즙 방울 이펙트
- 콤보/점수, 실패 없는 힐링 모드

## 깃허브 자동 빌드 설정 (1회)
1. 이 폴더를 그대로 깃허브 저장소에 push
2. Settings → Secrets and variables → Actions 에 3개 등록
   - `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`
   - 라이선스 발급 방법: https://game.ci/docs/github/activation
3. push 하면 Actions가 APK를 빌드 → 실행 페이지 하단 **Artifacts → HealingSlices-Android** 에서 다운로드
4. `git tag v1.0 && git push --tags` 하면 Releases 페이지에도 APK가 올라갑니다

## 로컬에서 열기
Unity Hub로 폴더 추가(2022.3.45f1) → 메뉴 `Tools > Create Main Scene` → Play
