# 통합 불량 판정 Threshold 분석

## 목적

이 프로젝트의 목표는 모든 마스크에 대해 측정점을 찍고, 결과를 GOOD/BAD로 분류하여 검토할 수 있게 하는 것이다.

따라서 마스크가 작거나 비율이 이상하다는 이유만으로 점 대응을 중단하지 않는다.

현재 BAD 판정은 하나의 스크롤 값으로 조절된다.

```text
Defect score >= 16 => BAD
```

## 기존 방식에서 바꾼 점

기존에는 다음 기준이 따로 있었다.

- `shape broken`
- `large island`
- `shape distorted`

하지만 `shape broken`은 마스크가 작거나 비율이 이상한 경우에도 점을 찍어야 하는 프로젝트 목표와 맞지 않았다.

그래서 `shape broken` 판정은 제거하고, 실제 결점에 해당하는 값만 하나의 점수로 합쳤다.

## 현재 BAD 판정

현재는 세 가지 결점 점수를 계산한다.

1. island score
2. distortion score
3. local defect score

그리고 이 중 가장 큰 값을 최종 결점 점수로 사용한다.

```csharp
DefectScore = Math.Max(IslandScore, Math.Max(DistortionScore, LocalDefectScore));
```

최종 판정은 하나의 threshold로 한다.

```csharp
if (DefectScore >= DefectThreshold)
{
    IsBad = true;
}
```

즉, 스크롤 값을 낮추면 더 민감하게 BAD가 되고, 높이면 더 관대하게 GOOD으로 통과한다.

## 1. Island Score

island는 본체 밖에 떨어진 흰색 조각이다.

OpenCV의 connected component 분석으로 흰색 component를 찾는다.

가장 큰 component는 제품 본체로 보고 제외한다.

나머지 component 중 가장 큰 bounding box 크기를 `IslandScore`로 사용한다.

```csharp
int box = Math.Max(componentWidth, componentHeight);
IslandScore = maxIslandBox;
```

예를 들어 본체 밖에 가로/세로 최대 크기 20px짜리 흰 조각이 있으면 island score는 20 근처가 된다.

## 2. Distortion Score

distortion은 형상 전체가 찌그러지거나 크게 파인 정도이다.

본체 contour 면적과 convex hull 면적을 비교한다.

```csharp
DistortionScore = (1.0 - contourArea / hullArea) * 100.0;
```

의미는 다음과 같다.

| 값 | 의미 |
|---|---|
| 낮음 | 실제 형상이 hull에 가깝고 매끈함 |
| 높음 | 외곽이 크게 파이거나 찌그러짐 |

convex hull은 마스크 외곽을 바깥에서 고무줄처럼 감싼 볼록 외곽이다.

외곽이 많이 파이면 실제 면적은 작아지고 hull 면적은 상대적으로 커져서 distortion score가 증가한다.

## 3. Local Defect Score

local defect는 작은 검은 구멍, 가장자리 찍힘, 작은 파임을 잡기 위한 점수이다.

기존 distortion score는 전체 면적 차이를 보므로 작은 결점에는 둔할 수 있다.

이를 보완하기 위해 convex hull 내부에서 실제 마스크가 비어 있는 검은 영역을 defect mask로 만든다.

```text
defect mask = convex hull mask - actual component mask
```

그 defect mask에서 connected component를 찾고, 가장 큰 결점 component의 크기를 점수화한다.

```csharp
score = max(width, height) + sqrt(area)
```

이렇게 하면 작은 검은 점이나 가장자리 결손도 일정 크기 이상이면 BAD로 잡을 수 있다.

## 최종 Reason 선택

BAD가 되었을 때는 세 점수 중 가장 큰 원인을 reason으로 표시한다.

| 가장 큰 점수 | 표시 reason |
|---|---|
| `IslandScore` | `large island` |
| `LocalDefectScore` | `local defect` |
| `DistortionScore` | `shape distorted` |

## 스크롤 조절 방법

UI에는 하나의 스크롤만 있다.

```text
Defect score >= N => BAD
```

| 스크롤 값 | 효과 |
|---|---|
| 낮게 설정 | 작은 island, 작은 검은 결점, 약한 찌그러짐도 BAD |
| 높게 설정 | 큰 결점만 BAD, 작은 결점은 GOOD |

기본값은 `16`이다.

작은 검은 점이나 가장자리 찍힘이 GOOD으로 남으면 값을 낮추면 된다.

정상 마스크가 BAD로 너무 많이 빠지면 값을 높이면 된다.

## 점 대응과 BAD 판정의 관계

BAD 판정은 결과를 분류하기 위한 것이다.

점 대응 자체를 막지 않는다.

따라서 `BAD - local defect` 또는 `BAD - large island`로 분류되어도 가능한 경우 빨간 측정점은 계속 계산되어 표시된다.

## CSV 로그 값

`match_log.csv`에는 다음 값이 저장된다.

| 컬럼 | 의미 |
|---|---|
| `islands` | threshold 이상인 island 개수 |
| `island_score` | 본체 밖 island 중 가장 큰 크기 |
| `distortion_score` | convex hull 대비 외곽 찌그러짐 점수 |
| `local_defect_score` | hull 내부 검은 결손 점수 |
| `defect_score` | 세 점수 중 최댓값 |
| `defect_threshold` | 실행 당시 통합 threshold |

## 보고서용 요약

본 프로그램은 불량 마스크를 island, 외곽 찌그러짐, 국소 결점 세 기준으로 분석한다.

각 기준은 `IslandScore`, `DistortionScore`, `LocalDefectScore`로 계산되며, 최종 BAD 판정에는 세 값 중 가장 큰 `DefectScore`를 사용한다.

사용자는 하나의 스크롤로 `DefectThreshold`를 조절할 수 있다.

이 방식은 여러 threshold를 따로 조절하는 것보다 단순하며, 결점 종류가 달라도 하나의 민감도 값으로 GOOD/BAD 분류를 조절할 수 있다.
