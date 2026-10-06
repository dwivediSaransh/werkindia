# Angular 21 implementation patterns

Use standalone components, lazy routes, Signals for local feature state and RxJS for async streams/interoperability. Do not create NgModules.

## Feature structure

```text
features/candidate/jobs/
  data-access/
  ui/
  pages/
  models/
  candidate-jobs.routes.ts
```

## Component starter

```ts
@Component({
  standalone: true,
  selector: "iw-example",
  imports: [CommonModule],
  templateUrl: "./example.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExampleComponent {
  private readonly api = inject(ExampleApi);
  readonly loading = signal(false);
}
```

## GraphQL rule

Operations live beside the consuming feature as `.graphql` documents. Generated files live under `src/app/generated/graphql/` and are never hand-edited.

## State rule

Keep server state in Apollo/cache or a feature data service. Use Signals for view state and current filters. Do not create a global store until a concrete cross-feature requirement exists.

## Mobile-first

Candidate screens must work first at 360–430px widths. Employer/admin screens must still be responsive but may use desktop layouts.
