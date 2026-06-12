# ADR-0001: DataTable Primary Workflow Is Editor Importer

상태: Accepted

날짜: 2026-06-03

## 배경

INLO Core DataTable에는 두 흐름이 있습니다.

- Editor Importer: xlsx 또는 Google Published CSV를 `DataTableAsset<TRow>`로 가져옵니다.
- Runtime `CsvDataTableParser`: 타입 행이 있는 CSV 문자열을 직접 파싱해 메모리 `DataTable`을 만듭니다.

두 흐름의 CSV 형식은 다릅니다. Editor Importer는 C# Row 타입이 스키마이고, runtime parser는 2행 타입 선언이 스키마입니다.

## 결정

게임 데이터의 primary workflow는 Editor Importer입니다.

Runtime `CsvDataTableParser`는 secondary API로 둡니다.

## 이유

- Unity 프로젝트에서는 ScriptableObject asset 기반 workflow가 inspector, serialization, build 안정성과 잘 맞습니다.
- C# Row 타입이 스키마 역할을 하면 필드 타입, optional/required attribute, enum 변환을 자연스럽게 사용할 수 있습니다.
- 런타임 CSV 파싱은 네트워크, 파일 접근, 초기화 순서, 플랫폼 차이 때문에 기본 경로로 두기 어렵습니다.
- Google Spreadsheet는 원본 편집 도구이며 런타임 의존성이 아닙니다.

## 결과

- 문서와 팀 가이드는 Editor Importer를 먼저 설명합니다.
- Runtime parser 문서는 별도 secondary workflow로 분리합니다.
- 새 기능은 `DataTableAsset<TRow>` workflow와 충돌하지 않아야 합니다.

## 금지

- Runtime Google Spreadsheet request 추가 금지.
- Runtime parser를 primary game data loading path로 홍보 금지.
- Editor Importer와 runtime parser의 CSV 형식을 하나처럼 설명 금지.
