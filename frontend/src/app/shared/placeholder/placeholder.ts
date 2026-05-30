import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

/** Stand-in for screens that land in a later Phase 12 chunk; keeps the shell navigable. */
@Component({
  selector: 'app-placeholder',
  imports: [MatCardModule, MatIconModule],
  template: `
    <mat-card appearance="outlined" class="placeholder">
      <mat-icon class="placeholder-icon">construction</mat-icon>
      <h2>{{ title }}</h2>
      <p>This screen lands in a later Phase 12 chunk.</p>
    </mat-card>
  `,
  styles: `
    .placeholder {
      max-width: 480px;
      margin: 48px auto;
      padding: 32px;
      text-align: center;
    }
    .placeholder-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
      color: var(--mat-sys-outline);
    }
    h2 {
      margin: 16px 0 4px;
    }
    p {
      color: var(--mat-sys-on-surface-variant);
      margin: 0;
    }
  `,
})
export class Placeholder {
  private readonly route = inject(ActivatedRoute);
  protected readonly title = (this.route.snapshot.data['title'] as string | undefined) ?? 'Coming soon';
}
