# OpenCV / OpenCvSharp 사용 멤버 정리

## 사용 패키지

| 패키지 | 버전 | 용도 |
|---|---:|---|
| `OpenCvSharp4` | `4.13.0.20260531` | C#에서 OpenCV API 사용 |
| `OpenCvSharp4.Extensions` | `4.13.0.20260531` | OpenCvSharp 확장 패키지 |
| `OpenCvSharp4.runtime.win` | `4.13.0.20260531` | Windows용 OpenCV native runtime |
| `OpenCvSharp4.Windows` | `4.13.0.20260531` | Windows 환경 OpenCvSharp 지원 |

## 핵심 OpenCV 함수

| 멤버 | 사용 위치 | 입력/출력 | 프로젝트에서의 역할 |
|---|---|---|---|
| `Cv2.ImRead(path, ImreadModes.Unchanged)` | `MaskAnalyzer.Analyze` | 파일 경로 -> `Mat` | PNG 마스크 이미지를 원본 채널 그대로 읽음. 알파 채널이 있는 이미지도 유지 |
| `Cv2.Split(source)` | `MaskAnalyzer.ToBinary` | 다채널 `Mat` -> 채널별 `Mat[]` | BGRA 이미지에서 alpha 채널을 따로 분리 |
| `Cv2.CvtColor(source, dst, ColorConversionCodes.BGRA2BGR)` | `MaskAnalyzer.ToBinary` | BGRA -> BGR | 알파 포함 이미지를 일반 BGR 이미지로 변환 |
| `Cv2.CvtColor(source, dst, ColorConversionCodes.BGR2GRAY)` | `MaskAnalyzer.ToBinary` | BGR -> Gray | RGB/BGR 이미지를 흑백 영상으로 변환 |
| `Cv2.Threshold(src, dst, 30, 255, ThresholdTypes.Binary)` | `MaskAnalyzer.ToBinary` | Gray/Alpha -> Binary | 픽셀값 30 이상을 흰색, 그 외를 검정으로 만들어 마스크화 |
| `Cv2.CountNonZero(alphaMask)` | `MaskAnalyzer.ToBinary` | Binary `Mat` -> count | alpha mask에 투명 영역이 있는지 확인 |
| `Cv2.BitwiseAnd(gray, alphaMask, gray)` | `MaskAnalyzer.ToBinary` | binary images -> masked image | alpha가 투명한 부분을 최종 마스크에서 제거 |
| `Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(...))` | `MaskAnalyzer.Analyze` | kernel shape/size -> `Mat` kernel | morphology 연산에 사용할 타원형 커널 생성 |
| `Cv2.MorphologyEx(gray, cleaned, MorphTypes.Open, spikeKernel)` | `MaskAnalyzer.Analyze` | Binary -> Binary | 작은 돌출/노이즈를 제거 |
| `Cv2.MorphologyEx(cleaned, cleaned, MorphTypes.Close, smoothKernel)` | `MaskAnalyzer.Analyze` | Binary -> Binary | 작은 구멍과 끊김을 메워 본체를 매끄럽게 함 |
| `Cv2.ConnectedComponentsWithStats(...)` | `MaskAnalyzer.Analyze`, `CountLargeIslandComponents` | Binary -> labels/stats/centroids | 연결 성분을 찾아 본체와 island를 분리 |
| `Cv2.Compare(labels, bestLabel, component, CmpTypes.EQ)` | `MaskAnalyzer.Analyze` | label image -> binary component | 가장 큰 본체 label만 흰색인 binary mask 생성 |
| `Cv2.FindContours(component, out contours, ..., RetrievalModes.External, ContourApproximationModes.ApproxSimple)` | `MaskAnalyzer.CalculateDistortionScore` | Binary -> contours | 본체 외곽선 추출 |
| `Cv2.ContourArea(contour)` | `MaskAnalyzer.CalculateDistortionScore` | contour -> area | 실제 외곽선 면적 및 hull 면적 계산 |
| `Cv2.ConvexHull(bestContour)` | `MaskAnalyzer.CalculateDistortionScore` | contour -> convex hull contour | 찌그러진 부분을 바깥에서 감싼 볼록 외곽 계산 |
| `Cv2.FillConvexPoly(hullMask, hull, Scalar.White)` | `MaskAnalyzer.CalculateLocalDefectScore` | hull contour -> filled mask | convex hull 내부를 흰색으로 채운 마스크 생성 |
| `Cv2.Subtract(hullMask, component, defectMask)` | `MaskAnalyzer.CalculateLocalDefectScore` | two masks -> difference mask | hull 내부인데 실제 마스크에는 없는 검은 결손 영역 추출 |

## OpenCvSharp 자료형

| 자료형 | 사용 위치 | 의미 | 프로젝트에서의 역할 |
|---|---|---|---|
| `Mat` | `MaskAnalyzer`, `ShapeFeatures`, `ShapeMatcher` | OpenCV 이미지/행렬 객체 | 원본 이미지, 이진 마스크, label image, morphology kernel 저장 |
| `Point2f` | `ShapeModel`, `ShapeFeatures`, `ShapeMatcher`, `MaskAnalyzer`, `ResultRenderer` | float 좌표점 | PCA 축, 중심점, 마스크 픽셀 좌표, 선분 좌표 계산 |
| `Point` / `OpenCvSharp.Point` | `MaskAnalyzer`, `ShapeMatcher` | int 좌표점 | contour 점, boundary 점 저장 |
| `Rect` | `ShapeFeatures`, `ShapeModel`, `MaskAnalyzer` | 사각형 영역 | bounding box 저장 |
| `Size` | `MaskAnalyzer` | 너비/높이 | morphology kernel 크기 지정 |
| `HierarchyIndex` | `MaskAnalyzer.CalculateDistortionScore` | contour 계층 정보 | `FindContours` 호출 시 계층 출력값으로 사용 |
| `Scalar` | `MaskAnalyzer.CalculateLocalDefectScore` | 색상/채움 값 | hull mask를 검정/흰색으로 초기화 및 채움 |

## OpenCV Enum / 상수

| 멤버 | 사용 위치 | 의미 |
|---|---|---|
| `ImreadModes.Unchanged` | `Cv2.ImRead` | 이미지 채널을 변환하지 않고 그대로 읽음 |
| `ColorConversionCodes.BGRA2BGR` | `Cv2.CvtColor` | 4채널 BGRA를 3채널 BGR로 변환 |
| `ColorConversionCodes.BGR2GRAY` | `Cv2.CvtColor` | BGR 컬러를 grayscale로 변환 |
| `ThresholdTypes.Binary` | `Cv2.Threshold` | threshold 이상은 max value, 미만은 0 |
| `MorphShapes.Ellipse` | `Cv2.GetStructuringElement` | 타원형 morphology kernel |
| `MorphTypes.Open` | `Cv2.MorphologyEx` | erosion 후 dilation. 작은 노이즈 제거 |
| `MorphTypes.Close` | `Cv2.MorphologyEx` | dilation 후 erosion. 작은 구멍 메움 |
| `PixelConnectivity.Connectivity8` | `Cv2.ConnectedComponentsWithStats` | 8방향 연결 기준 |
| `MatType.CV_32S` | `Cv2.ConnectedComponentsWithStats` | label image를 32-bit signed int로 저장 |
| `ConnectedComponentsTypes.Area` | `stats.Get<int>` | connected component 면적 컬럼 |
| `ConnectedComponentsTypes.Width` | `stats.Get<int>` | connected component bounding box 너비 컬럼 |
| `ConnectedComponentsTypes.Height` | `stats.Get<int>` | connected component bounding box 높이 컬럼 |
| `CmpTypes.EQ` | `Cv2.Compare` | 같은 값인지 비교 |
| `RetrievalModes.External` | `Cv2.FindContours` | 가장 바깥 외곽선만 추출 |
| `ContourApproximationModes.ApproxSimple` | `Cv2.FindContours` | 외곽선 점을 단순화하여 저장 |

## Mat 멤버

| 멤버 | 사용 위치 | 역할 |
|---|---|---|
| `new Mat()` | 여러 곳 | 빈 OpenCV 행렬 생성 |
| `Mat.Empty()` | `MaskAnalyzer.Analyze`, `ShapeMatcher` | 이미지/마스크가 비었는지 확인 |
| `Mat.Channels()` | `MaskAnalyzer.ToBinary` | 이미지 채널 수 확인 |
| `Mat.Rows`, `Mat.Cols` | `MaskAnalyzer`, `ShapeMatcher` | 이미지 높이/너비 반복문 범위로 사용 |
| `Mat.Width`, `Mat.Height` | `MaskAnalyzer.ToBinary` | 이미지 크기 및 alpha mask 판정 |
| `Mat.At<byte>(y, x)` | `MaskAnalyzer`, `ShapeMatcher` | binary mask의 특정 픽셀 값 읽기 |
| `Mat.Get<int>(row, col)` | `MaskAnalyzer` | connected component stats 값 읽기 |
| `Mat.Clone()` | `MaskAnalyzer.BuildFeatures` | 본체 binary mask를 `ShapeFeatures.BinaryMask`에 저장 |
| `Mat.Dispose()` | `MaskAnalyzer` | native 메모리 해제 |

## 알고리즘별 사용 흐름

### 1. 마스크 이진화

| 단계 | 사용 멤버 | 설명 |
|---|---|---|
| 이미지 읽기 | `Cv2.ImRead` | PNG 파일을 `Mat`으로 읽음 |
| 채널 확인 | `Mat.Channels()` | alpha 포함 여부 확인 |
| alpha 분리 | `Cv2.Split` | BGRA의 alpha 채널 추출 |
| grayscale 변환 | `Cv2.CvtColor` | BGR/BGRA를 흑백으로 변환 |
| 이진화 | `Cv2.Threshold` | 제품 영역을 흰색으로 변환 |
| alpha 적용 | `Cv2.BitwiseAnd` | 투명 영역 제거 |

### 2. 본체 추출 및 island 판정

| 단계 | 사용 멤버 | 설명 |
|---|---|---|
| morphology kernel 생성 | `Cv2.GetStructuringElement` | Open/Close용 타원 커널 생성 |
| 작은 잡음 제거 | `Cv2.MorphologyEx(..., MorphTypes.Open, ...)` | 돌출 노이즈 제거 |
| 구멍 메움 | `Cv2.MorphologyEx(..., MorphTypes.Close, ...)` | 작은 빈 영역 보정 |
| 연결 성분 분석 | `Cv2.ConnectedComponentsWithStats` | 본체와 분리 조각을 label로 구분 |
| 본체 선택 | `ConnectedComponentsTypes.Area` | 가장 면적이 큰 label을 본체로 선택 |
| island 크기 판정 | `ConnectedComponentsTypes.Width`, `Height` | 본체 외 component의 bounding box가 threshold 이상인지 확인 |
| 본체 마스크 생성 | `Cv2.Compare(..., CmpTypes.EQ)` | 선택된 label만 binary mask로 변환 |

### 3. 형상 찌그러짐 판정

| 단계 | 사용 멤버 | 설명 |
|---|---|---|
| 외곽선 추출 | `Cv2.FindContours` | 본체 binary mask에서 contour 추출 |
| 실제 면적 계산 | `Cv2.ContourArea(bestContour)` | 실제 외곽선 면적 |
| 볼록 외곽 계산 | `Cv2.ConvexHull(bestContour)` | 움푹 들어간 부분을 감싼 hull 생성 |
| hull 면적 계산 | `Cv2.ContourArea(hull)` | hull 면적 |
| distortion 계산 | 직접 계산식 | `(1 - contourArea / hullArea) * 100` |

### 4. 국소 결점 판정

| 단계 | 사용 멤버 | 설명 |
|---|---|---|
| hull mask 생성 | `Cv2.FillConvexPoly` | convex hull 내부를 흰색으로 채움 |
| 결손 영역 추출 | `Cv2.Subtract` | hull mask에서 실제 component mask를 빼서 검은 결손 영역을 얻음 |
| 결손 component 분석 | `Cv2.ConnectedComponentsWithStats` | 작은 구멍/파임/찍힘의 크기를 계산 |

### 5. 점 대응 및 렌더링 보조

| 단계 | 사용 멤버 | 설명 |
|---|---|---|
| 좌표/축 저장 | `Point2f` | 중심점, PCA 축, 앞/뒤 발볼 선 저장 |
| bounding box 저장 | `Rect` | 결과 이미지에 cyan box를 그릴 때 사용 |
| mask 픽셀 검사 | `Mat.At<byte>` | 점이 마스크 안쪽인지 확인 |

## 보고서용 요약

이 프로젝트에서는 OpenCvSharp를 이용하여 마스크 이미지를 읽고, 이진화하고, 본체 영역과 불량 영역을 분석했다.

이미지 전처리에는 `ImRead`, `CvtColor`, `Threshold`, `MorphologyEx`를 사용했다.

본체 추출과 island 판정에는 `ConnectedComponentsWithStats`를 사용했으며, 가장 큰 component를 제품 본체로 보고 나머지 component를 island 후보로 분류했다.

형상 찌그러짐 판정에는 `FindContours`, `ContourArea`, `ConvexHull`을 사용했다. 실제 contour 면적과 convex hull 면적의 차이를 이용하여 찌그러짐 점수를 계산했다.

작은 검은 구멍이나 가장자리 찍힘은 `FillConvexPoly`와 `Subtract`로 hull 내부 결손 영역을 만든 뒤 connected component로 크기를 측정했다.

좌표 계산과 결과 표시에는 `Mat`, `Point2f`, `Point`, `Rect` 같은 OpenCvSharp 자료형을 사용했다.
