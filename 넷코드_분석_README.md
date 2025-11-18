# Unity NetCode for Entities 호환성 분석 - MeowDish 프로젝트

**분석 날짜:** 2025-11-18
**분석 대상:** 6개 ECS 시스템 + 14개 컴포넌트
**전체 평가:** 어려운 마이그레이션 필요 (약 153시간 예상)

---

## 📚 문서 구조

이 분석은 현재 MeowDish ECS 코드베이스가 Unity NetCode for Entities와 얼마나 호환되는지 평가하고, 멀티플레이어 지원을 위해 필요한 변경사항을 문서화합니다.

### 문서 목록:

1. **넷코드_분석_요약.md** ⭐ **여기서 시작하세요**
   - 경영진 요약
   - 3가지 크리티컬 블로커
   - 단계별 구현 일정
   - 빠른 참조 테이블

2. **넷코드_상세분석.md** 📖 **가장 포괄적**
   - 시스템별 라인 단위 분석
   - 파일 경로와 라인 번호가 포함된 구체적인 문제점
   - 각 문제에 대한 상세한 해결책
   - 컴포넌트 마이그레이션 가이드

3. **넷코드_아키텍처_비교.md** 📊 **시각적 참조**
   - 현재 vs 필요한 아키텍처 다이어그램
   - 데이터 흐름 비교
   - 컴포넌트 구조 변경사항
   - 시스템 실행 순서

4. **넷코드_구현_템플릿.md** 💻 **코드 준비됨**
   - 각 시스템별 드롭인 코드 템플릿
   - 이전/이후 코드 비교
   - 필요한 새 컴포넌트들
   - 마이그레이션 체크리스트 & 유닛 테스트 예제

5. **빠른시작_가이드.txt** 🚀 **구현 가이드**
   - 단계별 구현 체크리스트
   - 수정할 주요 파일들
   - 테스트 요구사항
   - 흔한 실수들

---

## ⚡ 빠른 시작

멀티플레이어 지원을 위한 코드베이스 마이그레이션을 시작하려면:

### 1단계: 현황 이해 (30분)
```bash
# 다음 순서대로 읽으세요:
1. 넷코드_분석_요약.md (15분)
2. 넷코드_아키텍처_비교.md (15분)
```

### 2단계: 상세 계획 검토 (1시간)
```bash
3. 넷코드_상세분석.md (30분)
4. 넷코드_구현_템플릿.md (30분)
```

### 3단계: 구현 시작 (1-2주)
```bash
5. 빠른시작_가이드.txt를 따라 Phase 1 구현
6. 단계별로 테스트하며 진행
```

---

## 🎯 핵심 요약

### 현재 상태:
- ✅ **좋은 ECS 아키텍처**: Burst 컴파일, 컴포넌트 기반 설계
- ✅ **결정론적 로직**: 예측 가능한 수학 연산
- ❌ **싱글 플레이어 전용**: 네트워크 개념 없음
- ❌ **로컬 입력만**: 소유권/권한 시스템 없음
- ❌ **GameObject 커플링**: 네트워크 복제 불가능

### 필요한 작업:
- **6개 시스템 중 4개 재작성** (67%)
- **3가지 크리티컬 블로커** 해결 필요
- **예상 작업 시간**: 6-8주 (1명 개발자 기준)
- **난이도**: 🔴 높음

---

## 🚨 3가지 크리티컬 블로커

### 1. 아이템 ID 생성 (`ContainerSystem.cs:89`)
```csharp
// 문제: 각 클라이언트가 독립적으로 ID 생성
private int _nextItemID = 1;

// 결과: 네트워크 동기화 불가능
Client A: Item ID=5
Client B: Item ID=5  ← 충돌!
Server: Item ID=6    ← 불일치!
```

### 2. 입력 처리 (`PlayerInputSystem.cs`)
```csharp
// 문제: 로컬 입력만, 소유권 개념 없음
Input.GetKey() → 즉시 실행

// 결과: "내 입력"과 "다른 플레이어 입력" 구분 불가
```

### 3. GameObject 아키텍처 (`ItemVisualSystem.cs`)
```csharp
// 문제: ECS 루프에서 GameObject 생성
GameObject.CreatePrimitive()
Dictionary<Entity, GameObject>

// 결과: 네트워크 복제 불가능
```

---

## 📊 시스템별 상태

| 시스템 | 상태 | 난이도 | 우선순위 |
|--------|------|--------|----------|
| **PlayerMovementSystem** | ⚠️ 수정 필요 | 중간 | P1 |
| **PlayerAnimationSystem** | ⚠️ 수정 필요 | 쉬움 | P2 |
| **PlayerInputSystem** | ❌ 재작성 | 어려움 | P1 |
| **InteractionSystem** | ❌ 재작성 | 어려움 | P3 |
| **ContainerSystem** | ❌ 재작성 | 어려움 | P2 |
| **ItemVisualSystem** | ❌ 재작성 | 어려움 | P4 |

---

## 🗓️ 예상 타임라인

| 단계 | 기간 | 인원 | 리스크 |
|------|------|------|--------|
| Phase 1: 기초 작업 | 1주 | 1명 | 낮음 |
| Phase 2: 입력 & 이동 | 2주 | 1명 | 중간 |
| Phase 3: 아이템 시스템 | 2주 | 1명 | 높음 |
| Phase 4: 렌더링 | 1-2주 | 1명 | 중간 |
| Phase 5: 상호작용 | 1-2주 | 1명 | 높음 |
| Phase 6: 테스트 | 1-2주 | 2명 | 높음 |
| **전체** | **6-8주** | **1-2명** | **중간** |

---

## 💡 추천 접근법

### ✅ 재사용 가능한 것:
- 핵심 ECS 아키텍처 (좋은 기반)
- PlayerMovementSystem 로직 (소유권 체크만 추가)
- PlayerAnimationSystem 로직 (입력 소스만 변경)
- PlayerStatsComponent (순수 데이터, 결정론적)
- ItemComponent & HoldableComponent (좋은 데이터 구조)

### ❌ 재작성 필요한 것:
- PlayerInputSystem (NetCode와 완전히 비호환)
- ContainerSystem (아이템 ID 생성 문제)
- InteractionSystem (서버 권한 없음)
- ItemVisualSystem (GameObject 커플링)
- 네트워크 아키텍처 (아직 존재하지 않음)

---

## 🎓 학습한 교훈

### ✅ 현재 코드의 좋은 점:
1. **Burst 컴파일** - PlayerMovementSystem이 [BurstCompile] 사용
2. **결정론적 수학** - Random 사용 없음, 예측 가능한 계산
3. **EntityCommandBuffer** - 지연 업데이트의 좋은 패턴
4. **컴포넌트 기반 설계** - 데이터와 로직 분리
5. **시스템 구조화** - 명확한 그룹과 의존성

### ❌ 수정이 필요한 점:
1. **MonoBehaviour 시스템** - PlayerInputSystem이 ISystem struct가 아님
2. **GameObject 커플링** - ItemVisualSystem이 GameObject 생성
3. **GameObject.Find()** - 신뢰성 낮고 느림
4. **네트워크 개념 없음** - 소유권, 권한, 예측 없음
5. **비결정론적 ID** - 클라이언트별 정적 카운터
6. **직접 상태 변경** - 검증이나 RPC 패턴 없음

---

## 📖 다음 단계

1. **읽기**: 넷코드_분석_요약.md (현황 이해)
2. **검토**: 넷코드_아키텍처_비교.md (필요한 변경사항 시각화)
3. **결정**: 6-8주 작업이 멀티플레이어 지원에 가치가 있는가?
4. **시작**: Phase 1 기초 작업 (넷코드_구현_템플릿.md 사용)
5. **반복**: 제공된 코드 예제와 함께 단계별 진행

---

## 🔗 관련 리소스

- [Unity NetCode for Entities 공식 문서](https://docs.unity3d.com/Packages/com.unity.netcode@latest)
- [NetCode 샘플 프로젝트](https://github.com/Unity-Technologies/EntityComponentSystemSamples)
- [Multiplayer 베스트 프랙티스](https://docs-multiplayer.unity3d.com/)

---

## 📞 지원

질문이나 도움이 필요하면:
- 각 문서의 상세 섹션 참조
- 코드 템플릿으로 구현 가이드 제공
- 단계별 체크리스트로 진행 상황 추적

**행운을 빕니다!** 🚀
