# Competition Game Deployment

GitHub Pages URL:

`https://tjdeo1102.github.io/ProjectArchive.github.io/competition-game/`

Unity WebGL 빌드는 이 폴더 안에 다음 구조로 배치합니다.

```text
competition-game/
  Build/
    ProjectT.loader.js
    ProjectT.data
    ProjectT.framework.js
    ProjectT.wasm
  StreamingAssets/  # 빌드에서 생성된 경우
  index.html
  game-loader.js
  game-site.css
```

Unity가 압축 확장자(`.unityweb`, `.gz`, `.br`)를 생성했다면 `game-loader.js`의
`dataUrl`, `frameworkUrl`, `codeUrl`을 실제 파일명에 맞게 변경합니다.

포트폴리오 루트와 게임 페이지 사이에는 링크를 추가하지 않습니다. 두 페이지는 같은
GitHub Pages 저장소와 도메인을 사용하지만 URL을 아는 경우에만 각각 접근합니다.
