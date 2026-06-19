# Ascent — Design System

The look is a **warm editorial outdoors** aesthetic: cream paper background (never
plain white), deep forest green + a terracotta accent, a high-contrast serif for
headings, soft tinted difficulty badges, and a subtle topographic-ring motif.

Light mode only — the brand palette is a fixed, warm set. Dark mode is out of scope.

Implementation: Bootstrap 5 for layout/grid + `wwwroot/css/ascent.css` (ships with
this packet) for all tokens and bespoke components. Apply the `ascent.*` classes
below; don't restyle from scratch.

---

## 1. Palette

| Token | Hex | Use |
|---|---|---|
| paper | `#F3EFE5` | page background |
| surface | `#FBFAF4` | cards, panels, inputs |
| surface-alt | `#E7E3D7` | image placeholders |
| border | `#E0DACA` | default 0.5px borders |
| border-strong | `#D8D2C0` | hover / emphasis borders |
| forest | `#2F4A2C` | primary buttons, logo, links |
| forest-dark | `#21301C` | headings |
| forest-deep | `#33502F` | featured panel fill |
| terracotta | `#B05E3B` | accent word, "Hard" badge |
| text | `#283524` | primary text |
| text-secondary | `#5F5C4D` | body copy |
| text-muted | `#8A8775` | eyebrows, labels, captions |

### Difficulty badge tints
The `DifficultyService` returns one of four labels. Map each to a badge class:

| Label | Class | bg | text |
|---|---|---|---|
| Easy | `diff-easy` | `#E4ECD1` | `#4A6B2F` |
| Moderate | `diff-moderate` | `#F2E7CE` | `#8A6A2A` |
| Hard | `diff-hard` | `#F1DECF` | `#A85A33` |
| Strenuous | `diff-strenuous` | `#EFD9D4` | `#9E4B3F` |

---

## 2. Typography
- **Display / headings:** Fraunces (serif), weight 500–600. Use italic for accent
  words (in terracotta). Negative letter-spacing (~-0.5px) on large headings.
- **Body / UI:** Inter, weight 400–500.
- **Eyebrows / labels:** Inter 500, uppercase, letter-spacing ~1.5–2px, muted color.
  Used for region labels (`OHRID`) and section eyebrows (`TRAILS NEAR YOU`).

Load both fonts in `_Layout.cshtml` `<head>`:
```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Fraunces:ital,opsz,wght@0,9..144,400;0,9..144,500;0,9..144,600;1,9..144,500;1,9..144,600&family=Inter:wght@400;500;600&display=swap" rel="stylesheet">
```

Scale: hero `clamp(34px,5vw,52px)`, h2 ~28px, card title ~19px, body 15–16px,
small 13px, label 11px.

---

## 3. Shape & spacing
- Radius: cards/panels 12–16px, buttons/inputs 8px, chips/pills 999px, badges 6px.
- Borders: 0.5px, `border` token (use `border-strong` on hover).
- Generous padding: section gutters 20–24px, card body 14–16px.
- No drop shadows. Depth comes from the offset tan card behind the featured panel,
  not box-shadow.

---

## 4. Icons
Tabler Icons via CDN (the mockups use these). Add to `<head>`:
```html
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/@tabler/icons-webfont@3/dist/tabler-icons.min.css">
```
Key icons: `ti-mountain` (logo, elevation, placeholder), `ti-route` (distance),
`ti-star` (rating), `ti-map-pin` (region), `ti-navigation` (use my location),
`ti-compass` (eyebrow pill), `ti-heart` (favorite), `ti-user` (sign in).

---

## 5. Components (reference markup)

### Navbar — role-aware
```html
<nav class="asc-navbar">
  <div class="container d-flex align-items-center py-3">
    <a class="asc-brand" href="/">
      <span class="mark"><i class="ti ti-mountain"></i></span>
      <span class="wm">Ascent<small>NORTH MACEDONIA</small></span>
    </a>
    <div class="ms-auto d-flex align-items-center gap-4">
      <a class="asc-nav-link" href="/Trails">Discover</a>
      <a class="asc-nav-link" href="/Trails/All">All trails</a>
      @* logged out *@
      <a class="btn-ascent" href="/Account/Login"><i class="ti ti-user"></i>Sign in</a>
      @* logged in: replace Sign in with My hikes / Dashboard + avatar.
         Guides also see "Add trail"; Admins see "Admin". *@
    </div>
  </div>
</nav>
```

### Trail card
```html
<a class="trail-card" href="/Trails/Details/@trail.Id">
  <div class="tc-media">
    @if (trail.PhotoUrl != null) {
      <img src="@trail.PhotoUrl" alt="@trail.Name">
    } else {
      <i class="ti ti-mountain placeholder"></i>
    }
    <span class="diff-badge diff-@diffClass">@trail.DifficultyLabel</span>
    <button class="tc-fav @(isFav ? "active" : "")"><i class="ti ti-heart"></i></button>
  </div>
  <div class="tc-body">
    <div class="tc-region"><i class="ti ti-map-pin"></i>@trail.RegionName</div>
    <h3 class="tc-title">@trail.Name</h3>
    <p class="tc-desc">@trail.ShortDescription</p>
    <div class="tc-stats">
      <span><i class="ti ti-route"></i>@trail.DistanceKm km</span>
      <span><i class="ti ti-mountain"></i>@trail.ElevationGainM m</span>
      <span><i class="ti ti-star"></i>@trail.AverageRating</span>
    </div>
  </div>
</a>
```
`diffClass` = the label lowercased (`Easy`→`easy`, etc.).

### Hero (landing)
```html
<section class="asc-hero">
  <span class="asc-pill"><i class="ti ti-compass"></i>ASCENT · TRAILS REGISTER</span>
  <h1>The mountains of <em>North Macedonia</em>, hike by hike.</h1>
  <p class="lead">A curated register of trails, auto-rated for difficulty from
     distance and climb.</p>
  <div class="d-flex gap-2">
    <a class="btn-ascent" href="/Trails">Browse all trails</a>
    <a class="btn-ascent-outline" href="#"><i class="ti ti-navigation"></i>Use my location</a>
  </div>
</section>
```

### Featured panel (offset tan card + topographic rings)
```html
<div class="asc-featured">
  <div class="fp-shadow"></div>
  <div class="fp-panel">
    <partial name="_TopoRings" />
    <div class="fp-strip">
      <p class="eyebrow mb-1">FEATURED PEAK</p>
      <h3 class="mb-0" style="font-size:18px">Mt. Korab · 2,764 m</h3>
      <p class="mb-0 text-muted" style="font-size:13px">The highest summit in the country</p>
    </div>
  </div>
</div>
```

### Filter chips
```html
<a class="asc-chip active" href="#"><i class="ti ti-navigation"></i>My location</a>
<a class="asc-chip" href="?region=Skopje">Skopje</a>
```

### `_TopoRings.cshtml` partial (the signature motif)
```html
<svg class="fp-topo" viewBox="0 0 600 360" preserveAspectRatio="xMidYMax slice" aria-hidden="true">
  <g fill="none" stroke="#F3EFE5" stroke-width="1" opacity="0.10">
    <circle cx="300" cy="380" r="90"/>  <circle cx="300" cy="380" r="150"/>
    <circle cx="300" cy="380" r="210"/> <circle cx="300" cy="380" r="270"/>
    <circle cx="300" cy="380" r="330"/> <circle cx="300" cy="380" r="390"/>
  </g>
</svg>
```

---

## 6. Imagery
Trail photos are the visual backbone. Full-bleed (`object-fit: cover`) in the card
media area and trail-details hero. For seed/demo data use free Unsplash mountain
photos. Missing photo → the `surface-alt` placeholder with a `ti-mountain` glyph.

## 7. Accessibility
- All interactive elements get a visible focus ring (built into `ascent.css`).
- Every trail photo needs descriptive `alt` text (the trail name).
- Difficulty is never conveyed by color alone — the badge always shows the label
  text too.
- Maintain heading order (one `h1` per page).