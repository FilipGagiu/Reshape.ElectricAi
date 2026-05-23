import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';

export type SelectableCardLayout = 'list' | 'grid';

@Component({
    selector: 'app-selectable-card',
    imports: [TranslocoModule],
    templateUrl: './selectable-card.component.html',
    styleUrl: './selectable-card.component.css',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SelectableCardComponent {
    readonly labelKey = input.required<string>();
    readonly sublabelKey = input<string | null>(null);
    readonly icon = input<string | null>(null);
    readonly selected = input<boolean>(false);
    readonly disabled = input<boolean>(false);
    readonly multi = input<boolean>(false);
    readonly layout = input<SelectableCardLayout>('list');

    readonly toggle = output<void>();

    protected readonly isPrimeIcon = computed(() => {
        const value = this.icon();
        return value !== null && value.startsWith('pi-');
    });
    protected readonly isGrid = computed(() => this.layout() === 'grid');

    protected handleClick(): void {
        if (this.disabled()) {
            return;
        }
        this.toggle.emit();
    }
}
