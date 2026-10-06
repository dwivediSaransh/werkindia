import { Component, input } from '@angular/core';

type ErrorKind = 'server' | 'client' | 'authorization' | 'not-found';

@Component({
  selector: 'iw-error',
  standalone: true,
  templateUrl: './error.component.html',
  styleUrl: './error.component.scss',
})
export class ErrorComponent {
  readonly kind = input<ErrorKind>('server');
  readonly message = input<string | null>(null);
}
