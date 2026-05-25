import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ApiStatus {
  message: string;
  timestampUtc: string;
}

export interface ValidationIssue {
  code: string;
  message: string;
  location?: string;
}

export interface OverdueLoan {
  loanId: string;
  bookId: string;
  bookTitle: string;
  employeeId: string;
  borrowerName: string;
  dueDate: string;
  daysOverdue: number;
}

export interface OverdueLoansResponse {
  validationIssues: ValidationIssue[];
  data: OverdueLoan[];
}

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly backendBaseUrl = 'http://localhost:5256';

  constructor(private readonly http: HttpClient) {}

  getStatus(): Observable<ApiStatus> {
    return this.http.get<ApiStatus>(`${this.backendBaseUrl}/api/status`);
  }

  getOverdueLoans(): Observable<OverdueLoansResponse> {
    return this.http.get<OverdueLoansResponse>(`${this.backendBaseUrl}/api/library/overdue_loans`);
  }
}
