# PcControllTermProject

마스크 판별 과제 결과를 분석하고 시각적으로 확인하기 위한 도구입니다.

## 구성

- `analyze_labeled_results.py`: 라벨링 결과 파일을 읽어서 정확도, 가설 검정, 포인트 오차 요약을 생성합니다.
- `labeled_results_visual_tool.py`: 브라우저에서 결과 파일을 드래그 앤 드롭해 시각적으로 확인하는 도구입니다.
- `build_check/`: 실행 확인용 빌드 산출물과 필요한 DLL 파일이 들어 있습니다.

## 준비물

- Python 3
- `.xlsx` 파일을 읽거나 `.xlsx` 결과를 만들려면 `openpyxl`이 필요합니다.

```powershell
pip install openpyxl
```

`openpyxl`이 없어도 `.csv`와 Excel 호환 `.xls` 요약 파일은 생성됩니다. 다만 입력이 `.xlsx`인 경우에는 `openpyxl`이 반드시 필요합니다.

## 분석 스크립트 실행

CSV, Excel 호환 `.xls`, `.xlsx` 형식의 실험 결과 파일을 입력으로 사용할 수 있습니다.

```powershell
python analyze_labeled_results.py --input "결과파일경로.csv" --output "분석결과.xlsx"
```

예시:

```powershell
python analyze_labeled_results.py --input "C:\data\experiment_results.csv" --output "C:\data\hypothesis_test_results.xlsx"
```

실행하면 `--output`으로 지정한 위치를 기준으로 다음 파일이 함께 생성됩니다.

- `hypothesis_test_results.xlsx`: 엑셀 요약 파일
- `hypothesis_test_results_summary.xls`: Excel 호환 XML 형식 요약 파일
- `hypothesis_test_results_summary.csv`: CSV 요약 파일
- `hypothesis_test_results_summary.md`: Markdown 요약 파일

콘솔에는 분석 완료 메시지, 입력/출력 경로, 방향 판별 정확도, 평균 포인트 오차가 출력됩니다.

## 시각화 도구 실행

브라우저에서 결과 파일을 직접 확인하려면 다음 명령을 실행합니다.

```powershell
python labeled_results_visual_tool.py
```

실행 후 브라우저가 자동으로 열립니다. 화면에 `experiment_results.csv` 또는 Export Labels에서 나온 Excel 호환 `.xls` 파일을 드래그 앤 드롭하면 방향, 좌우, GOOD/BAD, 포인트 오차를 확인할 수 있습니다.

브라우저가 자동으로 열리지 않으면 터미널에 출력된 `http://127.0.0.1:포트번호/` 주소를 직접 열면 됩니다.

## 빌드된 실행 파일

빌드 확인용 실행 파일은 아래 경로에 있습니다.

```text
build_check\MidTermproject_21100210.exe
```

같은 폴더와 하위 `dll\x64` 폴더에 실행에 필요한 DLL이 함께 있으므로, 실행 파일만 따로 옮기지 말고 `build_check` 폴더 구조를 유지해야 합니다.

## GitHub 업로드

현재 프로젝트는 Git으로 관리되며 GitHub 저장소 `entalopy/PcControllTermProject`의 `main` 브랜치에 업로드되어 있습니다.
