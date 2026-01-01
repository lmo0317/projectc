# 버프 아이콘 이미지 생성 프롬프트

## 게임 스타일
- **테마**: Retro Synthwave / Cyberpunk
- **색상**: Neon Purple, Pink, Cyan 계열
- **배경**: Dark gradient or transparent
- **해상도**: 512x512px (권장: 1024x1024px)

---

## 1. Damage (데미지 증가) 버프
**색상**: 빨간색/주황색 (Red/Orange)

```
A glowing neon sword or dagger icon in retro synthwave style, bright red and orange gradient with electric sparks, sharp angular geometric design, cyberpunk weapon symbol, dark background with pink grid pattern, 1980s video game aesthetic, neon outline glow effect, simple and bold design, game UI icon, 512x512px
```

**핵심 요소**: 검/단검 + 빨간 네온 + 번개 효과

---

## 2. Speed (이동 속도) 버프
**색상**: 청록색/하늘색 (Cyan/Light Blue)

```
Futuristic speed wings or rocket boosters icon in synthwave style, bright cyan and light blue neon gradient, motion blur trails, streamlined aerodynamic shape, cyberpunk speed symbol with geometric lines, dark starfield background, 1980s retro-futuristic aesthetic, glowing edges, game UI icon, 512x512px
```

**핵심 요소**: 날개/부스터 + 청록색 + 모션 블러

---

## 3. Fire Rate (연사력) 버프
**색상**: 노란색/금색 (Yellow/Gold)

```
Rapid fire energy bullets or laser barrage icon in retro synthwave style, golden yellow and amber neon colors, multiple projectile trails, circular firing pattern, cyberpunk ammunition symbol, dark background with purple grid, 1980s arcade game aesthetic, bright glowing effect, game UI icon, 512x512px
```

**핵심 요소**: 다발 총알/레이저 + 노란색 + 원형 패턴

---

## 4. Missile Count (미사일 개수) 버프
**색상**: 보라색/마젠타 (Purple/Magenta)

```
Sleek homing missile icon in synthwave cyberpunk style, vibrant purple and magenta neon gradient, rocket with glowing trail, triangular warhead design, futuristic weapon symbol with circuit patterns, dark space background, 1980s sci-fi aesthetic, intense neon glow, game UI icon, 512x512px
```

**핵심 요소**: 미사일 + 보라색 + 유도 궤적

---

## 5. Max Health (최대 체력) 버프
**색상**: 분홍색/빨간색 (Pink/Red)

```
Glowing health crystal or heart with shield icon in retro synthwave style, bright pink and red neon gradient, geometric crystalline structure, protective energy barrier, cyberpunk life symbol with hexagonal patterns, dark background, 1980s video game aesthetic, pulsing glow effect, game UI icon, 512x512px
```

**핵심 요소**: 하트/크리스탈 + 분홍색 + 보호막

---

## 6. Critical (크리티컬) 버프
**색상**: 노란색/흰색 (Yellow/White)

```
Explosive star burst or lightning bolt icon in synthwave style, bright yellow and white neon colors, sharp angular explosion pattern, critical hit symbol with radiating energy lines, cyberpunk impact effect, dark gradient background, 1980s retro aesthetic, intense flash glow, game UI icon, 512x512px
```

**핵심 요소**: 폭발/번개 + 노란/흰색 + 방사 효과

---

## 7. Magnet (자석) 버프
**색상**: 초록색/청록색 (Green/Cyan)

```
Futuristic magnetic field or horseshoe magnet icon in retro synthwave style, bright green and cyan neon gradient, electromagnetic wave patterns, circular attraction field with arrows, cyberpunk magnetism symbol, dark background with grid lines, 1980s sci-fi aesthetic, glowing pulse effect, game UI icon, 512x512px
```

**핵심 요소**: 자석/전자기장 + 초록색 + 파동 패턴

---

## 8. Health Regen (체력 재생) 버프
**색상**: 라이트 그린/민트색 (Light Green/Mint)

```
Healing energy spiral or medical cross with plus symbol in synthwave style, bright mint green and light green neon gradient, rotating energy helix pattern, regeneration symbol with particle effects, cyberpunk medical icon, dark background, 1980s retro-futuristic aesthetic, soft pulsing glow, game UI icon, 512x512px
```

**핵심 요소**: 십자/나선 + 연두색 + 회복 파티클

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
