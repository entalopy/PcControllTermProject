# 회전 및 좌우 점 대응 수정 정리

## 기존 문제

입력 마스크는 실제로는 회전된 신발인데, 매칭 결과에서 빨간 측정점이 좌우가 뒤집힌 것처럼 찍히는 문제가 있었다.

원인은 두 가지였다.

1. PCA로 얻은 장축/단축은 방향 부호가 임의로 정해진다.
2. 기준 형상은 오른발인데, 마스크에 점을 찍을 때 `SideRatio`의 좌우 부호가 기준 오른발의 화면상 좌우와 반대로 적용되는 경우가 있었다.

그래서 오른발 마스크인데도 점 2/3, 4/5가 서로 반대편에 찍혀 미러처럼 보였다.

## 앞뒤 방향 보정

앞뒤 방향은 사용자 점 번호를 사용하지 않고 형상 자체에서 판단한다.

마스크의 PCA 장축 양 끝을 비교해서, 더 넓은 쪽을 앞발볼 쪽으로 판단한다.

```csharp
double positiveEndWidth = EstimateEndWidth(points, center, axisX, axisY, 1);
double negativeEndWidth = EstimateEndWidth(points, center, axisX, axisY, -1);
int rawFrontSign = positiveEndWidth >= negativeEndWidth ? 1 : -1;
```

기준 모델의 `T` 좌표는 `T=0`이 앞쪽, `T=1`이 뒤쪽이므로, 마스크의 `AxisX`도 앞에서 뒤로 흐르도록 맞춘다.

```csharp
axisX = Multiply(axisX, -rawFrontSign);
axisY = Multiply(axisY, -rawFrontSign);
```

이렇게 하면 신발이 180도 회전되어 있어도 앞발볼 폭을 기준으로 앞뒤가 다시 정렬된다.

## 오른발 점 배치 보정

기준 형상은 오른발이다.

따라서 `RIGHT`로 판단된 마스크는 기준 오른발을 그대로 회전시킨 것처럼 점이 찍혀야 한다. 별도의 mirror 변환을 하면 안 된다.

문제는 이미지 좌표계에서는 Y가 아래로 증가하기 때문에, 마스크의 단축 방향(`AxisY`)에 `SideRatio`를 그대로 적용하면 화면상 좌우가 반대로 보일 수 있다는 점이다.

그래서 오른발로 판단된 경우에는 점 배치에 사용하는 `SideRatio` 부호만 반대로 적용했다.

```csharp
private static float GetPointSideRatioForShoe(ShapeFeatures maskFeatures, float referenceSideRatio)
{
    if (string.Equals(maskFeatures.ShoeSide, "RIGHT", StringComparison.OrdinalIgnoreCase))
    {
        return -referenceSideRatio;
    }

    return referenceSideRatio;
}
```

이 처리는 mirror 후보를 다시 추가한 것이 아니다.

앞뒤 축은 회전으로 맞추고, 오른발 기준 좌우 부호만 화면 좌표계에 맞게 보정한 것이다.

## 좌우 발 판단

좌우 발은 먼저 앞쪽 방향을 확정한 뒤 판단한다.

1. 앞발볼 폭으로 앞코 방향을 찾는다.
2. 앞코 방향 기준으로 중간 아치가 어느 쪽으로 들어갔는지 계산한다.
3. 앞코 방향과 아치 방향의 외적 부호로 `LEFT` 또는 `RIGHT`를 정한다.

```csharp
Point2f innerDirection = Multiply(sideAxis, innerSign);
double screenCross = frontAxis.X * innerDirection.Y - frontAxis.Y * innerDirection.X;
return screenCross > 0 ? "LEFT" : "RIGHT";
```

이렇게 해야 180도 회전된 마스크에서도 단순히 화면 왼쪽/오른쪽만 보고 오판하지 않는다.

## 결과

- 오른발 마스크는 기준 오른발을 회전시킨 위치에 빨간점이 찍힌다.
- 왼발 마스크는 왼발로 표시되고, 회전 방향은 그대로 유지된다.
- mirror 후보를 사용하지 않으므로, 외곽선 점수 때문에 좌우가 뒤집히는 문제가 줄어든다.
- 상태 배지에 `LEFT / RIGHT / UNKNOWN`과 회전각을 표시하여 결과를 눈으로 확인할 수 있다.
