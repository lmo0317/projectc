# 버프 아이콘 이미지 생성 프롬프트

## 게임 스타일
- **테마**: Retro Synthwave / Cyberpunk
- **색상**: Neon Purple, Pink, Cyan 계열
- **배경**: 심플한 우주 배경 (검은 우주 + 별)
- **해상도**: 512x512px (권장: 1024x1024px)

---

## 1. Damage (데미지 증가) 버프
**색상**: 빨간색/주황색 (Red/Orange)

```
A glowing neon sword icon in retro synthwave style, bright red and orange gradient with electric sparks, sharp angular blade design, simple and clean geometric shape, cyberpunk weapon symbol, dark space background with stars, 1980s video game aesthetic, intense neon glow effect, game UI icon, 512x512px
```

**핵심 요소**: 검 + 빨간 네온 + 번개 효과

---

## 2. Speed (이동 속도) 버프
**색상**: 청록색/하늘색 (Cyan/Light Blue)

```
Simple triple arrows pointing forward icon in synthwave style, bright cyan and light blue neon gradient, clean geometric chevron design, motion speed lines, minimalist cyberpunk symbol, dark space background with stars, 1980s retro aesthetic, glowing neon edges, game UI icon, 512x512px
```

**핵심 요소**: 세 개 화살표 + 청록색 네온 + 심플한 디자인

---

## 3. Fire Rate (연사력) 버프
**색상**: 노란색/금색 (Yellow/Gold)

```
Three right-pointing arrows in a row icon in retro synthwave style, golden yellow and amber neon gradient, three identical chevron arrows (→ → →) aligned horizontally from left to right with equal spacing, each arrow glowing with motion speed lines showing rapid consecutive firing, simple geometric arrow shapes, cyberpunk rapid fire rate symbol, dark space background with stars, 1980s arcade game aesthetic, bright neon glow with speed effect, game UI icon, 512x512px
```

**핵심 요소**: 화살표 3개 (→→→) + 가로 일렬 배치 + 속도선 효과 + 노란색 네온

---

## 4. Missile Count (미사일 개수) 버프
**색상**: 노란색/금색 (Yellow/Gold)

```
Three yellow bullet projectiles bundled together icon in synthwave style, golden yellow and amber neon gradient, three cylindrical bullets grouped or overlapping in triangular formation without any border or frame, simple geometric bullet shapes closely stacked, clean borderless design showing multiple ammunition count, futuristic ammo symbol, dark space background with stars, 1980s sci-fi aesthetic, intense neon glow, game UI icon, 512x512px
```

**핵심 요소**: 묶인 노란 총알 3개 + 삼각형 배치 + 테두리 없음 + 노란색 네온

---

## 5. Max Health (최대 체력) 버프
**색상**: 분홍색/빨간색 (Pink/Red)

```
Glowing heart with shield outline icon in retro synthwave style, bright pink and red neon gradient, simple geometric heart shape with protective border, clean cyberpunk life symbol, dark space background with stars, 1980s video game aesthetic, pulsing neon glow, game UI icon, 512x512px
```

**핵심 요소**: 하트 + 보호막 윤곽선 + 분홍색 네온

---

## 6. Critical (크리티컬) 버프
**색상**: 노란색/흰색 (Yellow/White)

```
Star burst explosion icon in synthwave style, bright yellow and white neon colors, simple four-point star with radiating lines, clean geometric impact symbol, cyberpunk critical effect, dark space background with stars, 1980s retro aesthetic, intense flash glow, game UI icon, 512x512px
```

**핵심 요소**: 4방향 별 폭발 + 노란/흰색 + 방사선

---

## 7. Magnet (자석) 버프
**색상**: 초록색/청록색 (Green/Cyan)

```
Simple horseshoe magnet icon with visible magnetic field in retro synthwave style, bright green and cyan neon gradient, clean U-shaped magnetic symbol surrounded by glowing circular or oval attraction field area with medium opacity (60-70% alpha), electromagnetic range indicator clearly visible, small attraction particles floating around, minimalist cyberpunk design, dark space background with stars, 1980s sci-fi aesthetic, strong neon glow effect, game UI icon, 512x512px
```

**핵심 요소**: U자형 자석 + 영향 범위 원형 필드 (알파 60-70%) + 초록색 네온

---

## 8. Health Regen (체력 재생) 버프
**색상**: 라이트 그린/민트색 (Light Green/Mint)

```
Medical cross plus symbol with rotating circle icon in synthwave style, bright mint green and light green neon gradient, simple geometric cross design with circular energy ring, clean cyberpunk medical symbol, dark space background with stars, 1980s retro aesthetic, soft pulsing glow, game UI icon, 512x512px
```

**핵심 요소**: 십자 + 회전 링 + 연두색 네온

---

## Unity 색상 코드 참고

```csharp
// BuffIconsUI.cs의 색상 매칭
BuffType.Damage       => new Color(1f, 0.3f, 0.3f)      // 빨강
BuffType.Speed        => new Color(0.3f, 0.8f, 1f)      // 청록
BuffType.FireRate     => new Color(1f, 0.8f, 0.3f)      // 노랑
BuffType.MissileCount => new Color(0.8f, 0.3f, 1f)      // 보라
BuffType.MaxHealth    => new Color(1f, 0.3f, 0.6f)      // 분홍
BuffType.Critical     => new Color(1f, 1f, 0.3f)        // 밝은 노랑
BuffType.Magnet       => new Color(0.3f, 1f, 0.5f)      // 초록
BuffType.HealthRegen  => new Color(1f, 0.5f, 0.5f)      // 연분홍
```

---

## 이미지 생성 팁

### z-image 사용 시:
1. 각 프롬프트를 복사하여 순서대로 생성
2. 해상도: 1024x1024px 권장 (고품질)
3. 배경 투명화 옵션 활성화 (PNG)

### 후처리:
1. **배경 제거**: 투명 배경 PNG로 변환
2. **외곽 글로우**: 네온 효과 강화
3. **대비 조정**: 어두운 배경에서 잘 보이도록

### Unity 임포트:
1. 생성된 PNG 파일을 `Assets/Textures/BuffIcons/` 폴더에 저장
2. Texture Type: `Sprite (2D and UI)` 설정
3. Compression: `High Quality` 설정
4. BuffDataHelper에서 스프라이트 연결

---

## 파일명 권장 사항
- `BuffIcon_Damage.png`
- `BuffIcon_Speed.png`
- `BuffIcon_FireRate.png`
- `BuffIcon_MissileCount.png`
- `BuffIcon_MaxHealth.png`
- `BuffIcon_Critical.png`
- `BuffIcon_Magnet.png`
- `BuffIcon_HealthRegen.png`
