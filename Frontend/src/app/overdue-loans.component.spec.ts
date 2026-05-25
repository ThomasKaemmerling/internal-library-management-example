import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Subject, throwError } from 'rxjs';
import { ApiService, OverdueLoansResponse } from './api.service';
import { OverdueLoansComponent } from './overdue-loans.component';

describe('OverdueLoansComponent', () => {
  let fixture: ComponentFixture<OverdueLoansComponent>;
  let apiServiceMock: { getOverdueLoans: ReturnType<typeof vi.fn> };

  const sampleResponse: OverdueLoansResponse = {
    validationIssues: [{ code: 'missing_element', message: 'Some optional field is missing' }],
    data: [
      {
        loanId: 'L-100',
        bookId: 'B-10',
        bookTitle: 'Clean Code',
        employeeId: 'E001',
        borrowerName: 'Anna Schmidt',
        dueDate: '2026-05-10',
        daysOverdue: 15
      },
      {
        loanId: 'L-101',
        bookId: 'B-11',
        bookTitle: 'Domain-Driven Design',
        employeeId: 'E002',
        borrowerName: 'Max Mustermann',
        dueDate: '2026-05-12',
        daysOverdue: 13
      }
    ]
  };

  beforeEach(async () => {
    apiServiceMock = {
      getOverdueLoans: vi.fn(() => new Subject<OverdueLoansResponse>().asObservable())
    };

    await TestBed.configureTestingModule({
      imports: [OverdueLoansComponent],
      providers: [{ provide: ApiService, useValue: apiServiceMock }]
    }).compileComponents();

    fixture = TestBed.createComponent(OverdueLoansComponent);
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should show loading first and render loans after API response', () => {
    const response$ = new Subject<OverdueLoansResponse>();
    apiServiceMock.getOverdueLoans.mockReturnValue(response$.asObservable());

    fixture.detectChanges();

    const native = fixture.nativeElement as HTMLElement;
    expect(native.textContent).toContain('Lade...');

    response$.next(sampleResponse);
    response$.complete();
    fixture.detectChanges();

    const rows = native.querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
    expect(native.textContent).toContain('Anna Schmidt');
    expect(native.textContent).toContain('Parser-Hinweise:');
  });

  it('should render an error message when API request fails', () => {
    apiServiceMock.getOverdueLoans.mockReturnValue(
      throwError(() => new Error('Backend unavailable'))
    );

    fixture.detectChanges();

    const native = fixture.nativeElement as HTMLElement;
    expect(native.textContent).toContain('Daten konnten nicht geladen werden');
    expect(native.querySelectorAll('tbody tr').length).toBe(0);
  });

  it('should filter by borrower name, employee id, and book title case-insensitive', () => {
    const response$ = new Subject<OverdueLoansResponse>();
    apiServiceMock.getOverdueLoans.mockReturnValue(response$.asObservable());

    fixture.detectChanges();
    response$.next(sampleResponse);
    response$.complete();
    fixture.detectChanges();

    const native = fixture.nativeElement as HTMLElement;
    const filterInput = native.querySelector('#borrowerFilter') as HTMLInputElement;
    expect(filterInput).toBeTruthy();

    filterInput.value = 'anna';
    filterInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(native.querySelectorAll('tbody tr').length).toBe(1);
    expect(native.textContent).toContain('Anna Schmidt');

    filterInput.value = 'e002';
    filterInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(native.querySelectorAll('tbody tr').length).toBe(1);
    expect(native.textContent).toContain('Max Mustermann');

    filterInput.value = 'clean code';
    filterInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(native.querySelectorAll('tbody tr').length).toBe(1);
    expect(native.textContent).toContain('Anna Schmidt');
  });

  it('should trigger a new request when reload is clicked', () => {
    const first$ = new Subject<OverdueLoansResponse>();
    const second$ = new Subject<OverdueLoansResponse>();
    apiServiceMock.getOverdueLoans
      .mockReturnValueOnce(first$.asObservable())
      .mockReturnValueOnce(second$.asObservable());

    fixture.detectChanges();
    expect(apiServiceMock.getOverdueLoans).toHaveBeenCalledTimes(1);

    // Complete first load so the reload button is enabled.
    first$.next(sampleResponse);
    first$.complete();
    fixture.detectChanges();

    const native = fixture.nativeElement as HTMLElement;
    const reloadButton = native.querySelector('button') as HTMLButtonElement;
    expect(reloadButton.disabled).toBe(false);
    reloadButton.click();
    fixture.detectChanges();

    expect(apiServiceMock.getOverdueLoans).toHaveBeenCalledTimes(2);
  });
});