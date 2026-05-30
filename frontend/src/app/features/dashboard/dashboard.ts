import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

interface StatCard {
  readonly label: string;
  readonly value: string;
  readonly icon: string;
}

@Component({
  selector: 'app-dashboard',
  imports: [MatCardModule, MatIconModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Dashboard {
  // Placeholder figures — bound to the API in the next Phase 12 chunk (requests/approvals).
  protected readonly stats: readonly StatCard[] = [
    { label: 'My open requests', value: '—', icon: 'description' },
    { label: 'Awaiting my approval', value: '—', icon: 'task_alt' },
    { label: 'In fulfilment', value: '—', icon: 'inventory_2' },
    { label: 'Completed', value: '—', icon: 'check_circle' },
  ];
}
