# Carbon & Copper — Construction Signature Theme

**Premium | Compact | Elegant | Construction Industry**

## Design Philosophy

The Carbon & Copper theme delivers a premium, enterprise-focused visual experience tailored for the construction industry. It combines:

- **Dark Carbon Sidebar**: Professional dark navigation (#17212B) establishing authority and focus
- **Warm Light Content**: Comfortable neutral surfaces (#F8F6F2, #FFFFFF) for extended use
- **Copper Accents**: Strategic use of copper (#C46A36) for primary actions and highlights
- **Compact Layout**: Dense, efficient spacing for data-heavy enterprise workflows
- **Subtle Elegance**: Refined typography, minimal shadows, and thoughtful transitions

---

## Design Tokens

### Brand Colors

```scss
--primary-copper: #C46A36        // Primary action color
--primary-copper-hover: #A9562B  // Hover state
--primary-copper-light: #D98558  // Light variant
--primary-copper-pale: #F5E6DD   // Very light accent fill
```

### Sidebar — Dark Carbon

```scss
--sidebar-bg: #17212B            // Main sidebar background
--sidebar-hover: #223140         // Hover state
--sidebar-active: #2A3A4D        // Active item
--sidebar-border: rgba(255, 255, 255, 0.08)
```

### Surfaces & Backgrounds

```scss
--canvas: #F8F6F2                // Page background (warm neutral)
--surface: #FFFFFF               // Card/panel background
--surface-hover: #FAFAF9         // Hover state
--surface-raised: #FFFFFF        // Elevated components
--surface-accent: #F5E6DD        // Soft copper accent fill
--overlay: rgba(23, 33, 43, 0.6) // Modal overlay
```

### Text Colors

```scss
--text-heading: #1F2933          // Headings and emphasized text
--text-primary: #1F2933          // Body text
--text-secondary: #5B6773        // Secondary text
--text-muted: #7C8893            // Muted/subtle text
--text-disabled: #A0AEC0         // Disabled state
--text-inverse: #FFFFFF          // Text on dark backgrounds
```

### Borders

```scss
--border-light: #F0EDE8          // Very light borders
--border-default: #E6DDD4        // Default border color
--border-medium: #D4CAC0         // Medium borders (hover)
--border-strong: #A0948A         // Strong borders
```

### Semantic Colors

```scss
--success: #2F855A
--warning: #D97706
--danger: #C24130
--info: #2B6CB0
```

---

## Typography

**Font Family**: Inter (system fallback: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto)

### Font Sizes (Compact Scale)

```scss
--font-size-xs: 0.6875rem    // 11px - smallest text
--font-size-sm: 0.8125rem    // 13px - base UI text
--font-size-base: 0.875rem   // 14px - body text
--font-size-md: 0.9375rem    // 15px - medium text
--font-size-lg: 1rem         // 16px - section headings
--font-size-xl: 1.125rem     // 18px - page headings
--font-size-2xl: 1.25rem     // 20px - large headings
--font-size-3xl: 1.5rem      // 24px - hero headings
```

### Font Weights

```scss
--font-weight-normal: 400
--font-weight-medium: 500
--font-weight-semibold: 600
--font-weight-bold: 700
```

---

## Spacing Scale

Compact 4px-based spacing for dense enterprise layouts:

```scss
--space-1: 0.25rem   // 4px
--space-2: 0.5rem    // 8px
--space-3: 0.75rem   // 12px
--space-4: 1rem      // 16px
--space-5: 1.25rem   // 20px
--space-6: 1.5rem    // 24px
--space-8: 2rem      // 32px
--space-10: 2.5rem   // 40px
--space-12: 3rem     // 48px
```

---

## Component Heights

Compact enterprise sizing:

```scss
--height-input: 2rem           // 32px - form inputs
--height-button: 1.875rem      // 30px - standard buttons
--height-button-sm: 1.625rem   // 26px - small buttons
--height-button-lg: 2.25rem    // 36px - large buttons
--height-table-row: 2.25rem    // 36px - table rows
--height-header: 3.25rem       // 52px - main header
```

---

## Shadows

Subtle, elegant shadows for professional appearance:

```scss
--shadow-sm: 0 1px 2px 0 rgba(31, 41, 51, 0.04)
--shadow-md: 0 2px 6px 0 rgba(31, 41, 51, 0.06)
--shadow-lg: 0 4px 12px 0 rgba(31, 41, 51, 0.08)
--shadow-xl: 0 8px 24px 0 rgba(31, 41, 51, 0.1)
```

---

## Border Radius

Subtle, professional rounding:

```scss
--radius-sm: 0.25rem    // 4px
--radius-md: 0.375rem   // 6px
--radius-lg: 0.5rem     // 8px
--radius-xl: 0.625rem   // 10px
--radius-full: 9999px   // Fully rounded
```

---

## Usage Guidelines

### Sidebar Navigation

- **Background**: Dark carbon (#17212B) for professional contrast
- **Active Items**: Copper accent (#C46A36) with subtle shadow
- **Hover**: Slightly lighter carbon (#223140)
- **Icons**: 18px with proper alignment
- **Typography**: 13px medium weight

### Content Area

- **Background**: Warm neutral (#F8F6F2) reduces eye strain
- **Cards**: Pure white (#FFFFFF) with subtle border
- **Headers**: Heading color (#1F2933) at 16px semibold
- **Body Text**: Primary color (#1F2933) at 14px

### Buttons

- **Primary**: Copper (#C46A36) for main actions
- **Secondary**: White with border for alternate actions
- **Ghost**: Transparent for tertiary actions
- **Danger**: Red (#C24130) for destructive actions
- **Height**: 30px standard, 26px small, 36px large

### Forms

- **Labels**: Heading color at 13px medium weight
- **Inputs**: 32px height, copper focus ring
- **Error State**: Danger color with 10% background tint
- **Spacing**: 16px between fields (--space-4)

### Tables

- **Header**: Canvas background, uppercase 11px semibold
- **Rows**: 36px height for compact data display
- **Hover**: Canvas background on row hover
- **Selected**: Copper pale background (#F5E6DD)
- **Borders**: Light warm borders (#F0EDE8)

### Cards

- **Background**: White with default border
- **Padding**: 20px (--space-5) standard
- **Shadow**: Subtle md shadow on elevated cards
- **Top Accent**: 2px copper bar on hover (subtle)

---

## Color Usage Rules

### Copper Accent Strategy

**DO:**
- Use for primary action buttons
- Use for active navigation items
- Use for focus states and accents
- Use sparingly for maximum impact

**DON'T:**
- Use copper for large background areas
- Overuse copper throughout the UI
- Use copper for body text

### Warm Neutrals

The warm neutral palette (#F8F6F2, #E6DDD4) differentiates this from generic blue/gray themes and aligns with construction industry warmth.

### Construction Industry Feel

The combination of:
- Dark professional sidebar (like blueprints/technical drawings)
- Warm, earthy content area (construction materials)
- Copper accents (industrial metals, pipes, fixtures)

Creates a cohesive construction-industry aesthetic while maintaining enterprise professionalism.

---

## Accessibility

- **Contrast Ratios**: All text meets WCAG AA standards
- **Focus Indicators**: Clear copper focus rings (3px with 10% opacity)
- **Touch Targets**: Minimum 32px height for interactive elements
- **Color Independence**: Status conveyed through icons + color

---

## Implementation Notes

1. **Global Tokens**: All design tokens defined in `frontend/src/styles.scss`
2. **Component Styles**: Individual components use CSS variables for consistency
3. **Future Modules**: Import and use existing tokens; do not create new color schemes
4. **Dark Mode**: Not currently implemented; sidebar is intentionally dark by design
5. **Responsive**: Maintains theme across all breakpoints

---

## File Locations

- **Global Styles**: `frontend/src/styles.scss`
- **Sidebar**: `frontend/src/app/core/layout/sidebar/sidebar.component.ts`
- **Header**: `frontend/src/app/core/layout/header/header.component.ts`
- **Button**: `frontend/src/app/shared/ui/button/button.component.ts`
- **Card**: `frontend/src/app/shared/ui/card/card.component.ts`
- **Data Table**: `frontend/src/app/shared/ui/data-table/data-table.component.ts`
- **Form Field**: `frontend/src/app/shared/ui/form-field/form-field.component.ts`
- **Input**: `frontend/src/app/shared/ui/input/input.component.ts`
- **Status Chip**: `frontend/src/app/shared/ui/status-chip/status-chip.component.ts`

---

## Browser Support

- Chrome/Edge 90+
- Firefox 88+
- Safari 14+
- Modern mobile browsers

---

**Version**: 1.0.0  
**Last Updated**: November 2026  
**Maintained By**: Berynx Construction 360 Team
