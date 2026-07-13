import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ThemeService } from '../core/theme/theme.service';

@Component({
  selector: 'app-operations-shell',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './operations-shell.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OperationsShellComponent {
  readonly theme = inject(ThemeService);
}
