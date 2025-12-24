# MeowDish 

고양이 테마의 캐주얼 쿠킹/서빙 타이쿤 게임. DOTS(ECS)로 손님, 조리/서빙, 버프 시스템을 구현했습니다

## 개발 환경
- Unity 6000.0.58f1 (URP 17.0.4)
- Entities 1.3.14 + Physics, Input System 1.14.2
- 주요 씬: Assets/Scenes/Stage_Burger.unity

## 플레이
- 흐름: 손님 대기 → 주문 → 재료 픽업/손질/굽기/조합 → 서빙 → 점수/실패 체크
- 생명 모두 소모 시 Game Over, 모든 손님 처리 시 Stage Clear

## 조작
- 이동: WASD / 방향키, 혹은 조이패드
- 상호작용: E 혹은 상호작용 버튼 Tap

## 핵심 시스템
- DOTS 기반 게임 루프와 스테이션 인터랙션 (ECS/Systems)
- 레시피/아이템 파이프라인과 레시피 Blob
- 스테이지 & 보상: 스테이지 정의, 스킬 보상 선택
- 시각/사운드 브릿지: ECS 이벤트→파티클·UI·오디오

## 콘텐츠
- 레시피: 버거, 포장된 버거
- 스킬: 이동 속도/조리 속도 버프, 점수 보너스 등

## 빌드/실행
1) Unity Hub로 6000.0.58f1 버전 열기  
2) `Assets/Scenes/Lobby.unity`에서 Play (필요 시 RunDefinition SO로 스테이지/보상 테이블 설정)

## 스크린샷
<img width="742" height="416" alt="캡처_2025_12_22_17_05_10_534" src="https://github.com/user-attachments/assets/0056b259-9a7e-43c8-ae4e-ba38e90e12a1" />
<img width="742" height="416" alt="캡처_2025_12_22_17_15_15_817" src="https://github.com/user-attachments/assets/2b6b23b6-878b-41d2-8c3c-a6d7374019aa" />

