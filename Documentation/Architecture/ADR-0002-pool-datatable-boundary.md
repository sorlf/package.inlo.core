# ADR-0002: Pool and DataTable Are Independent

상태: Accepted

날짜: 2026-06-03

## 배경

Pool과 DataTable은 서로 다른 수명 주기와 설정 원천을 가진 시스템입니다. 두 시스템을 연결하면 DataTable 스키마가 Pool 설정에 종속되고 Pool 구성의 책임 경계가 흐려집니다.

## 결정

Pool과 DataTable은 runtime과 Editor tooling 모두에서 서로 참조하거나 자동 변환하지 않습니다.

Pool은 사람이 구성한 `PoolDatabase`를 설정 원천으로 사용합니다. DataTable은 Pool 타입과 Pool 전용 컬럼 규칙을 알지 않습니다.

## 이유

- Pool runtime은 빠르고 예측 가능한 `PoolDatabase` 등록에 집중해야 합니다.
- DataTable 스키마는 게임 데이터 요구에 따라 독립적으로 설계되어야 합니다.
- 자동 연결은 asset reference와 초기화 책임을 불명확하게 만듭니다.

## 결과

- Pool 설정은 `PoolDatabase`에서 명시적으로 관리합니다.
- DataTable import와 스키마 생성은 Pool 설정에 영향을 주지 않습니다.
- Pool Editor tooling은 DataTable을 검색하거나 후보로 해석하지 않습니다.

## 금지

- Pool runtime 또는 Editor tooling에서 DataTable을 참조하는 코드 금지.
- DataTable runtime 또는 Editor tooling에서 Pool 타입과 Pool 전용 컬럼 규칙을 참조하는 코드 금지.
- DataTable에서 PoolDatabase를 생성하거나 갱신하는 workflow 금지.
