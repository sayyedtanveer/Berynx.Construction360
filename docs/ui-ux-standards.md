# Berynx Construction 360 — UI/UX Design Standards

**Version:** 1.0  
**Last Updated:** November 2024  
**Status:** Active

---

## Overview

This document defines the design standards for Berynx Construction 360, a premium compact enterprise construction ERP. The design is inspired by modern enterprise SaaS products (Linear, IBM Carbon) while maintaining construction-domain functionality.

**Design Goals:**
- **Clean + Compact + Premium + Professional + Fast**
- Professional enterprise aesthetic
- Efficient use of screen real estate
- Consistent, predictable interactions
- Accessible to all users (WCAG 2.1 AA)

---

## Design Tokens

### Typography

**Font Family:**
```css
font-family: Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
```

**Font Sizes:**
- `--font-size-xs`: 11px (0.6875rem) - Helper text, labels
- `--font-size-sm`: 13px (0.8125rem) - **Base UI text** (buttons, inputs, tables)
- `--font-size-base`: 14px (0.875rem) - Body text
- `--font-size-lg`: 16px (1rem) - Section headings
- `--font-size-xl`: 18px (1.125rem) - Page headings
- `--font-size-2xl`: 20px (1.25rem) - Large metrics
- `--font-size-3xl`: 24px (1.5rem) - Hero text

**Font Weights:**
- `normal`: 400 - Body text
- `medium`: 500 - Emphasis
- `semibold`: 600 - Headings, labels
- `bold`: 700 - Page titles

### Color Palette

**Primary Colors:**
```css
--primary-navy: #1f3a5f      /* Brand color */
--action-blue: #2563eb       /* Interactive elements */
--canvas: #f6f8fb            /* Page background */
--surface: #ffffff           /* Card/panel background */
```

**Text Colors:**
```css
--text-primary: #0f172a      /* Headlines, body */
--text-secondary: #475569    /* Supporting text */
--text-muted: #64748b        /* Disabled, hints */
--text-inverse: #ffffff      /* On dark backgrounds */
```

**Semantic Colors:**
```css
--success: #059669           /* Completed, active */
--warning: #d97706           /* Pending, attention */
--danger: #dc2626            /* Errors, critical */
--info: #0284c7              /* Informational */
```

**Status Colors:**
- Draft: Grey (#64748b)
- Pending: Amber (#f59e0b)
- Approved: Blue (#3b82f6)
- Active/Completed: Green (#10b981)
- Rejected/Overdue: Red (#ef4444)
- Cancelled: Dark grey with strikethrough

**Borders:**
```css
--border-light: #f1f5f9
--border-default: #e2e8f0
--border-medium: #cbd5e1
```

### Spacing Scale

Based on 4px increment:
```css
--space-1: 4px    --space-6: 24px
--space-2: 8px    --space-8: 32px
--space-3: 12px   --space-10: 40px
--space-4: 16px   --space-12: 48px
--space-5: 20px   --space-16: 64px
```

### Component Heights

```css
--height-input: 36px         /* Form inputs */
--height-button: 32px        /* Standard button */
--height-button-sm: 28px     /* Small button */
--height-button-lg: 40px     /* Large button */
--height-table-row: 40px     /* Data table row */
--height-header: 56px        /* Top header bar */
```

### Border Radius

```css
--radius-sm: 4px
--radius-md: 6px
--radius-lg: 8px
--radius-xl: 12px
--radius-full: 9999px
```

### Shadows

```css
--shadow-sm: 0 1px 2px 0 rgba(15, 23, 42, 0.05)
--shadow-md: 0 2px 8px 0 rgba(15, 23, 42, 0.08)
--shadow-lg: 0 4px 16px 0 rgba(15, 23, 42, 0.1)
--shadow-xl: 0 8px 32px 0 rgba(15, 23, 42, 0.12)
```

---

## Layout Structure

### Application Shell

**Left Sidebar:**
- Width: 220px (expanded), 56px (collapsed)
- Fixed position, full height
- Grouped navigation by functional area
- Collapsible with icon-only mode

**Top Header:**
- Height: 56px
- Fixed position
- Contains: Breadcrumbs, tenant/branch selector, notifications, user menu
- Adapts to sidebar width

**Main Content:**
- Margin-left: 220px (desktop), 0 (mobile)
- Margin-top: 56px
- Background: `--canvas`
- Padding: 20px

### Page Layout

**Container Widths:**
- Maximum page width: 1440px (`--page-max-width`)
- Content width: 1200px (`--content-max-width`)
- Form max width: 800px

**Page Structure:**
```html
<div class="page-container">
  <div class="page-header">
    <div class="page-title">
      <h1>Page Title</h1>
      <p class="text-secondary">Description</p>
    </div>
    <div class="page-actions">
      <!-- Buttons -->
    </div>
  </div>
  <!-- Content -->
</div>
```

---

## Component Standards

### Buttons

**Variants:**
- **Primary**: Blue background, white text - Main actions
- **Secondary**: White background, border - Secondary actions
- **Ghost**: Transparent background - Tertiary actions
- **Danger**: Red background - Destructive actions

**Sizes:**
- Small: 28px height
- Medium (default): 32px height
- Large: 40px height

**Usage:**
- Primary action: One per screen section
- Use icons sparingly, only when they add clarity
- Loading state: Show spinner, disable button
- Disabled state: 50% opacity, no pointer events

### Form Controls

**Input Fields:**
- Height: 36px
- Border: 1px solid `--border-default`
- Border radius: 6px
- Font size: 13px
- Padding: 8px 12px
- Focus: Blue border + subtle shadow

**Form Field Structure:**
```html
<app-form-field label="Field Label" [required]="true" [error]="errorMessage">
  <app-input type="text" placeholder="Enter value" />
</app-form-field>
```

**Form Layouts:**
- Two-column grid on desktop (>1024px)
- Single column on mobile (<768px)
- Group related fields in sections
- Use `form-actions-sticky` for save/cancel buttons

### Data Tables

**Structure:**
- Compact rows: 40px height
- Sticky header on scroll
- Zebra striping optional
- Hover state for rows
- Font size: 13px

**Features Required:**
- Server-side pagination
- Column sorting (where applicable)
- Search/filter
- Empty state with action
- Loading skeleton
- Responsive: Horizontal scroll on mobile

**Column Types:**
- Text: Left-aligned
- Numbers/Currency: Right-aligned
- Status: Center-aligned chips
- Actions: Right-aligned, icon buttons

### Cards

**Usage:**
- Contain related content
- Group form sections
- Display metrics/stats
- Elevated cards for emphasis

**Structure:**
```html
<app-card [title]="'Card Title'" [elevated]="true">
  <div header-actions>
    <button>Action</button>
  </div>
  <!-- Content -->
  <div footer>
    <!-- Footer content -->
  </div>
</app-card>
```

### Status Chips

**Display:**
- Uppercase text, 11px
- Rounded pill shape
- Optional dot indicator
- Semantic colors by status

**Usage:**
- Always include status text (not color-only)
- Use consistently across all entity types
- Include aria-label for accessibility

### Modals/Dialogs

**Structure:**
- Centered overlay with backdrop
- Max width: 600px (small), 800px (medium), 1200px (large)
- Rounded corners: 12px
- Shadow: `--shadow-xl`

**Components:**
```html
<div class="modal-overlay">
  <div class="modal">
    <div class="modal-header">
      <h2 class="modal-title">Title</h2>
    </div>
    <div class="modal-body">
      <!-- Content -->
    </div>
    <div class="modal-footer">
      <button>Cancel</button>
      <button class="primary">Confirm</button>
    </div>
  </div>
</div>
```

---

## Navigation

### Sidebar Navigation

**Groups:**
1. **Dashboard** (no label)
2. **Organization**: Company setup, branches
3. **Access Control**: Users, roles, approvals, audit
4. **Masters**: Customers, suppliers, products, pricing, fleet, configuration
5. **Account**: Profile, MFA

**Behavior:**
- Active route highlighted with blue background
- Hover state for all items
- Expandable groups with chevron indicator
- Keyboard accessible (Tab, Arrow keys, Enter)

### Breadcrumbs

**Format:**
```
Home / Section / Current Page
```

**Styling:**
- Font size: 13px
- Separated by `/`
- Last item bold (current page)
- Linked items underline on hover

---

## States and Feedback

### Loading States

**Skeleton:**
- Use for initial page load
- Animated gradient shimmer
- Match layout of loaded content

**Spinner:**
- Use for button actions
- Use for inline updates
- Size: 14px (small), 24px (medium), 32px (large)

### Empty States

**Components:**
- Large icon or illustration (3rem, 50% opacity)
- Title (font-size-base, semibold)
- Description (font-size-sm, muted)
- Primary action button

**Example:**
```html
<div class="empty-state">
  <span class="empty-icon">📋</span>
  <p class="empty-title">No data yet</p>
  <p class="empty-message">Get started by creating a record</p>
  <button class="primary">Create New</button>
</div>
```

### Error States

**Inline Errors:**
- Below form field
- Red text (danger color)
- Icon: ⚠
- Font size: 11px

**Page-level Errors:**
- Notice component
- Red background (danger-light)
- Dismissible
- Clear action to resolve

### Success Feedback

**Toast Notifications:**
- Top-right corner
- Auto-dismiss after 5 seconds
- Green background (success-light)
- Optional undo action

**Inline Success:**
- Green notice component
- Checkmark icon
- Confirmation message

---

## Responsive Design

### Breakpoints

```css
Mobile:  < 768px
Tablet:  768px - 1024px
Desktop: > 1024px
```

### Mobile Adaptations

**Navigation:**
- Sidebar hidden by default
- Hamburger menu toggle
- Bottom navigation bar (optional)

**Forms:**
- Single column layout
- Full-width inputs
- Larger touch targets (44px minimum)

**Tables:**
- Horizontal scroll
- Sticky first column (optional)
- Card-based layout alternative

**Page Header:**
- Stack title and actions vertically
- Hide breadcrumbs
- Condensed user menu

---

## Accessibility

### WCAG 2.1 AA Compliance

**Color Contrast:**
- Text on background: 4.5:1 minimum
- Large text (18px+): 3:1 minimum
- UI components: 3:1 minimum

**Keyboard Navigation:**
- All interactive elements focusable
- Visible focus indicator (2px blue outline)
- Logical tab order
- Keyboard shortcuts documented

**Screen Readers:**
- Semantic HTML (`<nav>`, `<main>`, `<section>`)
- ARIA labels where needed
- Status updates announced
- Error messages associated with fields

**Forms:**
- Labels for all inputs
- Required fields marked
- Error messages descriptive
- Instructions clear

### Touch Targets

Minimum size: 44px x 44px for mobile

---

## Performance

### Guidelines

- **Page Load:** < 2 seconds
- **Search Response:** < 500ms
- **Table Load:** < 2 seconds for 50K rows (paginated)
- **Form Submission:** Immediate feedback, async processing

### Optimization

- Lazy load routes
- Virtual scrolling for large lists
- Image optimization
- Bundle size monitoring
- Server-side pagination

---

## Documentation

### Component Documentation

Each reusable component includes:
- Purpose and usage guidelines
- Props/inputs documentation
- Code examples
- Accessibility notes
- Do's and don'ts

### Design Tokens

Maintained in: `frontend/src/styles.scss`

Access: Use CSS variables everywhere
```css
color: var(--text-primary);
padding: var(--space-4);
```

---

## Implementation Checklist

### New Screen Checklist

- [ ] Uses page-container layout
- [ ] Page header with title and actions
- [ ] Loading state implemented
- [ ] Empty state with helpful message
- [ ] Error handling with user-friendly messages
- [ ] Form validation (if applicable)
- [ ] Responsive at all breakpoints
- [ ] Keyboard accessible
- [ ] Screen reader tested
- [ ] Status indicators use chips
- [ ] Data tables use DataTable component
- [ ] Forms use FormField + Input components
- [ ] Buttons use Button component
- [ ] Cards use Card component

### Component Checklist

- [ ] TypeScript with proper types
- [ ] Standalone component (Angular)
- [ ] Uses design tokens (no hardcoded values)
- [ ] Responsive styling
- [ ] Accessibility attributes
- [ ] Loading/disabled states
- [ ] Error state handling
- [ ] Documentation comment
- [ ] Reusable and composable

---

## Examples

### Complete Page Example

```typescript
@Component({
  selector: 'app-example-page',
  imports: [CommonModule, CardComponent, DataTableComponent, ButtonComponent],
  template: `
    <div class="page-container">
      <div class="page-header">
        <div class="page-title">
          <h1>Page Title</h1>
          <p class="text-secondary text-sm">Page description</p>
        </div>
        <div class="page-actions">
          <app-button variant="primary" (click)="onCreate()">
            Create New
          </app-button>
        </div>
      </div>

      @if (loading) {
        <div class="loading-container">
          <div class="loading-spinner"></div>
          <p>Loading...</p>
        </div>
      } @else if (error) {
        <div class="notice notice-error">{{ error }}</div>
      } @else {
        <app-card [elevated]="true">
          <app-data-table
            [columns]="columns"
            [data]="data"
            [loading]="tableLoading"
            (rowClick)="onRowClick($event)"
          />
        </app-card>
      }
    </div>
  `
})
export class ExamplePage {}
```

---

## Resources

### Design System Files

- **Tokens:** `frontend/src/styles.scss`
- **Components:** `frontend/src/app/shared/ui/`
- **Layout:** `frontend/src/app/core/layout/`

### External References

- **Typography:** [Inter Font](https://rsms.me/inter/)
- **Icons:** Emoji-based (consistent cross-platform)
- **Accessibility:** [WCAG 2.1](https://www.w3.org/WAI/WCAG21/quickref/)

---

## Changelog

### Version 1.0 (November 2024)
- Initial design system established
- Compact enterprise aesthetic defined
- Component library created
- Application shell redesigned
- Dashboard page implemented
- Documentation completed

---

**Maintained by:** Berynx Product & Engineering Team  
**Questions:** Refer to this document or consult the design system lead
