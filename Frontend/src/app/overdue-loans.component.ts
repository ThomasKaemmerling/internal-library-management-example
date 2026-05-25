import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { ApiService, OverdueLoan, ValidationIssue } from './api.service';
import { BehaviorSubject, Subject, catchError, combineLatest, map, of, shareReplay, startWith, switchMap } from 'rxjs';

@Component({
  selector: 'app-overdue-loans',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './overdue-loans.component.html',
  styleUrl: './overdue-loans.component.css'
})
export class OverdueLoansComponent {
  private readonly reloadTrigger = new Subject<void>();
  private readonly borrowerFilter = new BehaviorSubject<string>('');

  private readonly dataState$ = this.reloadTrigger.pipe(
    startWith(void 0),
    switchMap(() =>
      this.apiService.getOverdueLoans().pipe(
        map((result) => ({
          loading: false,
          error: null as string | null,
          loans: result.data,
          validationIssues: result.validationIssues ?? []
        })),
        startWith({
          loading: true,
          error: null as string | null,
          loans: [] as OverdueLoan[],
          validationIssues: [] as ValidationIssue[]
        }),
        catchError(() =>
          of({
            loading: false,
            error: 'Daten konnten nicht geladen werden. Bitte prüfen Sie, ob das Backend auf http://localhost:5256 läuft.',
            loans: [] as OverdueLoan[],
            validationIssues: [] as ValidationIssue[]
          })
        )
      )
    ),
    shareReplay({ bufferSize: 1, refCount: true })
  );

  protected readonly vm$ = combineLatest([this.dataState$, this.borrowerFilter]).pipe(
    map(([state, filterText]) => {
      const normalizedFilter = filterText.trim().toLocaleLowerCase();
      const containsCaseInsensitive = (value: string, searchTerm: string): boolean =>
        value.toLocaleLowerCase().includes(searchTerm);

      const filteredLoans = !normalizedFilter
        ? state.loans
        : state.loans.filter((loan) =>
            containsCaseInsensitive(loan.borrowerName, normalizedFilter) ||
            containsCaseInsensitive(loan.employeeId, normalizedFilter) ||
            containsCaseInsensitive(loan.bookTitle, normalizedFilter)
          );

      return {
        loading: state.loading,
        error: state.error,
        validationIssues: state.validationIssues,
        filteredLoans,
        borrowerFilter: filterText
      };
    })
  );

  constructor(private readonly apiService: ApiService) {
  }

  protected onFilterChange(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.borrowerFilter.next(value);
  }

  protected loadOverdueLoans(): void {
    this.reloadTrigger.next();
  }
}