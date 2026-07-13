import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { RuleApiClient } from '../../core/api/rule-api.client';
import { httpErrorMessage } from '../../core/http/problem-details';
import { PricingRule, RuleType } from '../../models/api.models';
import { reviewRuleCatalog } from '../../utilities/delivery-areas';
import { RuleEditorComponent } from './rule-editor.component';

@Component({
  selector: 'app-rules',
  imports: [RuleEditorComponent],
  templateUrl: './rules.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RulesComponent implements OnInit {
  private readonly ruleApi = inject(RuleApiClient);

  readonly rules = signal<PricingRule[]>([]);
  readonly catalogIssues = computed(() => reviewRuleCatalog(this.rules()));
  readonly loading = signal(true);
  readonly loadError = signal('');
  readonly isEditorOpen = signal(false);
  readonly editingRule = signal<PricingRule | null>(null);

  ngOnInit(): void {
    this.loadRules();
  }

  loadRules(): void {
    this.loading.set(true);
    this.ruleApi.listRules().subscribe({
      next: (rules) => {
        this.rules.set(rules);
        this.loading.set(false);
      },
      error: (error) => {
        this.loadError.set(httpErrorMessage(error));
        this.loading.set(false);
      },
    });
  }

  ruleTypeLabel(type: RuleType): string {
    return {
      WeightTier: 'Weight tier',
      RemoteAreaSurcharge: 'Area surcharge',
      TimeWindowPromotion: 'Promotion',
    }[type];
  }

  openEditor(rule: PricingRule | null = null): void {
    this.editingRule.set(rule);
    this.isEditorOpen.set(true);
  }

  closeEditor(): void {
    this.isEditorOpen.set(false);
  }

  ruleSaved(): void {
    this.closeEditor();
    this.loadRules();
  }

  removeRule(rule: PricingRule): void {
    if (!rule.id || !confirm(`Delete “${rule.name}”?`)) return;
    this.ruleApi.deleteRule(rule.id).subscribe({
      next: () => this.loadRules(),
      error: (error) => this.loadError.set(httpErrorMessage(error)),
    });
  }
}
