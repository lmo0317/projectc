# 버프 아이콘 생성 프롬프트 (z-image용)

## 게임 스타일 참고
- **장르**: 사이버펑크 레트로 우주 슈팅 게임
- **색상 톤**: 보라색 네온, 우주 블랙, 밝은 레이저 이펙트
- **분위기**: 80년대 아케이드 + 현대적 네온 그래픽

---

## 공통 스타일 가이드

모든 아이콘에 적용할 공통 요소:
- 크기: 128x128 또는 256x256 픽셀
- 배경: 투명 또는 어두운 보라색 (#1a0a2e)
- 스타일: 네온 글로우 효과, 플랫 디자인, 미니멀리스트
- 테두리: 둥근 사각형 또는 육각형 프레임 (선택)
- 색감: 게임의 보라색/핑크색 네온 톤과 어울리게

---

## 개별 아이콘 프롬프트

### 1. Damage (데미지 증가) - 빨강
**파일명**: `Damage.png`

```
Cyberpunk game icon, 128x128 pixels, neon red glowing crossed swords symbol, retro arcade style, dark purple background #1a0a2e, bright red neon glow effect (#ff4d4d), minimalist flat design, sci-fi weapon icon, transparent or dark background, clean sharp edges, 80s synthwave aesthetic
```

**대체 심볼**: 검, 폭발 마크, 화살촉

---

### 2. Speed (이동 속도) - 하늘색/시안
**파일명**: `Speed.png`

```
Cyberpunk game icon, 128x128 pixels, neon cyan glowing speed lines with arrow symbol, retro arcade style, dark purple background #1a0a2e, bright cyan neon glow effect (#4dd4ff), minimalist flat design, motion blur trail effect, sci-fi speed boost icon, transparent or dark background, 80s synthwave aesthetic
```

**대체 심볼**: 날개, 번개, 달리는 실루엣

---

### 3. FireRate (공격 속도) - 주황
**파일명**: `FireRate.png`

```
Cyberpunk game icon, 128x128 pixels, neon orange glowing triple bullet or rapid fire symbol, retro arcade style, dark purple background #1a0a2e, bright orange neon glow effect (#ffb84d), minimalist flat design, multiple projectile lines, sci-fi attack speed icon, transparent or dark background, 80s synthwave aesthetic
```

**대체 심볼**: 기관총, 연속 화살, 시계+총알

---

### 4. Missile (미사일 추가) - 보라
**파일명**: `Missile.png`

```
Cyberpunk game icon, 128x128 pixels, neon purple glowing triple rocket or missile symbol, retro arcade style, dark purple background #1a0a2e, bright magenta neon glow effect (#cc4dff), minimalist flat design, multiple missiles flying upward, sci-fi projectile count icon, transparent or dark background, 80s synthwave aesthetic
```

**대체 심볼**: 로켓 3개, 탄창, 폭발+숫자

---

### 5. Magnet (자석 범위) - 초록
**파일명**: `Magnet.png`

```
Cyberpunk game icon, 128x128 pixels, neon green glowing horseshoe magnet with magnetic field lines symbol, retro arcade style, dark purple background #1a0a2e, bright green neon glow effect (#4dff80), minimalist flat design, attraction force waves, sci-fi magnet pull icon, transparent or dark background, 80s synthwave aesthetic
```

**대체 심볼**: U자 자석, 원형 자기장, 끌어당기는 파동

---

### 6. HealthRegen (체력 재생) - 분홍
**파일명**: `HealthRegen.png`

```
Cyberpunk game icon, 128x128 pixels, neon pink glowing heart with plus sign and healing waves symbol, retro arcade style, dark purple background #1a0a2e, bright pink neon glow effect (#ff8080), minimalist flat design, pulsing heart regeneration, sci-fi health recovery icon, transparent or dark background, 80s synthwave aesthetic
```

**대체 심볼**: 하트+화살표 순환, 붕대, 회복 파동

---

### 7. MaxHealth (최대 체력) - 핫핑크
**파일명**: `MaxHealth.png`

```
Cyberpunk game icon, 128x128 pixels, neon hot pink glowing large shield with heart inside symbol, retro arcade style, dark purple background #1a0a2e, bright hot pink neon glow effect (#ff4d99), minimalist flat design, strong protective aura, sci-fi max HP icon, transparent or dark background, 80s synthwave aesthetic
```

**대체 심볼**: 큰 하트, 방패+하트, 체력바 MAX

---

### 8. Critical (치명타) - 노랑
**파일명**: `Critical.png`

```
Cyberpunk game icon, 128x128 pixels, neon yellow glowing crosshair target with exclamation mark or star burst symbol, retro arcade style, dark purple background #1a0a2e, bright yellow neon glow effect (#ffff4d), minimalist flat design, precision hit marker, sci-fi critical strike icon, transparent or dark background, 80s synthwave aesthetic
```

**대체 심볼**: 조준경+별, 폭발 마크, 해골+X

---

## 색상 참조 (HEX)

| 버프 | 메인 색상 | 글로우 색상 |
|------|-----------|-------------|
| Damage | #ff4d4d | #ff6666 |
| Speed | #4dd4ff | #66ddff |
| FireRate | #ffb84d | #ffcc66 |
| Missile | #cc4dff | #dd66ff |
| Magnet | #4dff80 | #66ff99 |
| HealthRegen | #ff8080 | #ff9999 |
| MaxHealth | #ff4d99 | #ff66aa |
| Critical | #ffff4d | #ffff66 |
| 배경 | #1a0a2e | #2a1a4e |

---

## 추가 팁

1. **일관성 유지**: 모든 아이콘에 동일한 글로우 강도와 선 두께 적용
2. **가독성**: 작은 크기에서도 심볼이 잘 보이도록 단순하게
3. **투명 배경**: 게임 UI에 자연스럽게 어울리도록 PNG 투명 배경 권장
4. **테스트**: 생성 후 게임 내에서 80x80 크기로 표시되므로 축소 테스트 필요

---

## 파일 저장 위치

```
Assets/Resources/Icons/Buffs/
├── Damage.png
├── Speed.png
├── FireRate.png
├── Missile.png
├── Magnet.png
├── HealthRegen.png
├── MaxHealth.png
└── Critical.png
```

Unity에서 이미지 Import 후 **Texture Type**을 **Sprite (2D and UI)** 로 설정하세요.
