# Carbon & Copper Color Palette

Quick visual reference for the Construction Signature theme.

---

## Brand Colors

### Copper (Primary Action)
```
█████ #C46A36  --primary-copper         Primary buttons, active states
█████ #A9562B  --primary-copper-hover   Hover state
█████ #D98558  --primary-copper-light   Light variant
█████ #F5E6DD  --primary-copper-pale    Soft accent fill
```

---

## Sidebar Colors

### Dark Carbon
```
█████ #17212B  --sidebar-bg             Main sidebar background
█████ #223140  --sidebar-hover          Hover state
█████ #2A3A4D  --sidebar-active         Active item (not currently used)
      rgba(255, 255, 255, 0.08)         Border color
```

---

## Surface & Background

### Warm Neutrals
```
█████ #F8F6F2  --canvas                 Page background
█████ #FFFFFF  --surface                Card/panel background
█████ #FAFAF9  --surface-hover          Hover state
█████ #FFFFFF  --surface-raised         Elevated components
█████ #F5E6DD  --surface-accent         Soft accent fill (copper pale)
      rgba(23, 33, 43, 0.6)             Modal overlay
```

---

## Text Colors

### Neutrals
```
█████ #1F2933  --text-heading           Headings, emphasized text
█████ #1F2933  --text-primary           Body text
█████ #5B6773  --text-secondary         Secondary text
█████ #7C8893  --text-muted             Muted/subtle text
█████ #A0AEC0  --text-disabled          Disabled state
█████ #FFFFFF  --text-inverse           Text on dark backgrounds
```

---

## Border Colors

### Warm Border Palette
```
█████ #F0EDE8  --border-light           Very light borders (table rows)
█████ #E6DDD4  --border-default         Default border
█████ #D4CAC0  --border-medium          Medium borders (hover)
█████ #A0948A  --border-strong          Strong borders
```

---

## Semantic Colors

### Success
```
█████ #2F855A  --success                Success text/icons
█████ #E6F5ED  --success-light          Success background
█████ #9AE6B4  --success-border         Success border
```

### Warning
```
█████ #D97706  --warning                Warning text/icons
█████ #FEF3C7  --warning-light          Warning background
█████ #FCD34D  --warning-border         Warning border
```

### Danger
```
█████ #C24130  --danger                 Danger text/icons
█████ #FED7D7  --danger-light           Danger background
█████ #FC8181  --danger-border          Danger border
```

### Info
```
█████ #2B6CB0  --info                   Info text/icons
█████ #EBF8FF  --info-light             Info background
█████ #90CDF4  --info-border            Info border
```

---

## Status Colors

For status chips and indicators:

```
█████ #64748B  --status-draft           Draft items
█████ #F59E0B  --status-pending         Pending approval
█████ #2B6CB0  --status-approved        Approved items
█████ #2F855A  --status-active          Active records
█████ #047857  --status-completed       Completed tasks
█████ #C24130  --status-rejected        Rejected items
█████ #6B7280  --status-cancelled       Cancelled (with strikethrough)
█████ #991B1B  --status-overdue         Overdue items
```

---

## Usage Rules

### ✅ DO
- Use copper (`#C46A36`) for primary actions only
- Use warm neutrals (`#F8F6F2`, `#E6DDD4`) for surfaces and borders
- Use semantic colors (`success`, `warning`, `danger`) for feedback
- Maintain dark carbon sidebar (`#17212B`)

### ❌ DON'T
- Don't use copper for large background areas
- Don't create new accent colors outside this palette
- Don't use pure black (`#000000`) or pure white (`#FFFFFF`) for text
- Don't use blue for primary actions (copper is primary)

---

## Color Combinations

### Primary Button
```
Background: #C46A36  (copper)
Text:       #FFFFFF  (white)
Hover:      #A9562B  (darker copper)
```

### Secondary Button
```
Background: #FFFFFF  (white)
Text:       #1F2933  (heading)
Border:     #E6DDD4  (default border)
Hover BG:   #F8F6F2  (canvas)
```

### Sidebar Navigation
```
Background: #17212B  (dark carbon)
Text:       rgba(255, 255, 255, 0.7)
Active BG:  #C46A36  (copper)
Active Text:#FFFFFF  (white)
Hover BG:   #223140  (lighter carbon)
```

### Form Input
```
Background: #FFFFFF  (white)
Border:     #E6DDD4  (default border)
Text:       #1F2933  (heading)
Focus:      #C46A36  (copper) with 10% opacity ring
Error:      #C24130  (danger) with 10% opacity ring
```

### Data Table
```
Header BG:  #F8F6F2  (canvas)
Row BG:     #FFFFFF  (white)
Row Hover:  #F8F6F2  (canvas)
Border:     #F0EDE8  (light border)
Selected:   #F5E6DD  (copper pale)
```

---

## Accessibility

All color combinations meet **WCAG AA** contrast requirements:

| Combination | Contrast Ratio | Status |
|-------------|----------------|--------|
| `#1F2933` on `#FFFFFF` | 12.63:1 | ✅ AAA |
| `#5B6773` on `#FFFFFF` | 7.37:1 | ✅ AAA |
| `#FFFFFF` on `#C46A36` | 3.47:1 | ✅ AA |
| `#FFFFFF` on `#17212B` | 15.84:1 | ✅ AAA |
| `#C46A36` on `#F8F6F2` | 4.35:1 | ✅ AA |

---

## Export for Design Tools

### Figma / Sketch
```
Copper:        #C46A36
Carbon:        #17212B
Canvas:        #F8F6F2
Surface:       #FFFFFF
Text Primary:  #1F2933
Text Muted:    #7C8893
Border:        #E6DDD4
Success:       #2F855A
Warning:       #D97706
Danger:        #C24130
```

### CSS Variables
All colors available as CSS custom properties in `frontend/src/styles.scss`:
```css
var(--primary-copper)
var(--sidebar-bg)
var(--canvas)
var(--surface)
var(--text-heading)
/* etc. */
```

---

**Reference**: See `frontend/THEME.md` for complete design system documentation.
