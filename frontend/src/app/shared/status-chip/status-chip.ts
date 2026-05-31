import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Semantic colour for a status chip — resolved to theme tokens in styles.scss. */
export type Tone = 'neutral' | 'info' | 'progress' | 'success' | 'danger';

/** A label paired with the tone it should render in. */
export interface ToneLabel {
  readonly label: string;
  readonly tone: Tone;
}

/**
 * Small, presentational status badge (a themed pill). Used for request status, approval
 * decisions, etc. Colour comes from the `--app-tone-*` theme tokens — never hardcoded.
 */
@Component({
  selector: 'app-status-chip',
  // Both classes in one binding — a `[class]` binding replaces the static class attribute,
  // so the base `chip` class is included here rather than via `class="chip"`.
  template: `<span [class]="'chip tone-' + tone()">{{ label() }}</span>`,
  styleUrl: './status-chip.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusChip {
  readonly label = input.required<string>();
  readonly tone = input<Tone>('neutral');
}
