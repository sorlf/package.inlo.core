# INLO Core Architecture Decisions

이 폴더는 INLO Core의 핵심 설계 결정을 짧게 기록합니다.

목적은 미래의 팀원과 AI 에이전트가 같은 논쟁을 반복하지 않게 만드는 것입니다.

## ADR 목록

| ADR | 제목 | 상태 |
| --- | --- | --- |
| ADR-0001 | DataTable primary workflow is Editor Importer | Accepted |
| ADR-0002 | Pool and DataTable are independent | Accepted |
| ADR-0003 | Event communication policy | Accepted |

## ADR 사용 규칙

- 큰 구조 판단을 바꾸려면 새 ADR을 추가하거나 기존 ADR을 갱신합니다.
- 모듈 README에는 결론을 적고, 이유는 ADR에 둡니다.
- AI가 금지 경계를 바꾸려 할 때는 관련 ADR을 먼저 읽습니다.
- ADR은 길게 쓰지 않습니다. 배경, 결정, 결과, 금지만 남깁니다.
