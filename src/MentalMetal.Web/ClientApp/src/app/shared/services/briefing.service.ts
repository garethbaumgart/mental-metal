import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PersonDossier } from '../models/dossier.model';
import { DailyBrief, WeeklyBrief } from '../models/briefing.model';

@Injectable({ providedIn: 'root' })
export class BriefingService {
  private readonly http = inject(HttpClient);

  // --- Person Dossier ---

  getDossier(personId: string, mode: 'default' | 'prep' = 'default', captureLimit = 20): Observable<PersonDossier> {
    let params = new HttpParams()
      .set('mode', mode)
      .set('captureLimit', captureLimit.toString());
    return this.http.get<PersonDossier>(`/api/people/${personId}/dossier`, { params });
  }

  refreshDossier(personId: string, mode: 'default' | 'prep' = 'default', captureLimit = 20): Observable<PersonDossier> {
    let params = new HttpParams()
      .set('mode', mode)
      .set('captureLimit', captureLimit.toString());
    return this.http.post<PersonDossier>(`/api/people/${personId}/dossier/refresh`, {}, { params });
  }

  // --- Daily Brief ---

  getDailyBrief(): Observable<DailyBrief> {
    return this.http.get<DailyBrief>('/api/briefing/daily');
  }

  refreshDailyBrief(): Observable<DailyBrief> {
    return this.http.post<DailyBrief>('/api/briefing/daily/refresh', {});
  }

  // --- Weekly Brief ---

  getWeeklyBrief(weekOf?: string): Observable<WeeklyBrief> {
    let params = new HttpParams();
    if (weekOf) {
      params = params.set('weekOf', weekOf);
    }
    return this.http.get<WeeklyBrief>('/api/briefing/weekly', { params });
  }

  refreshWeeklyBrief(weekOf?: string): Observable<WeeklyBrief> {
    let params = new HttpParams();
    if (weekOf) {
      params = params.set('weekOf', weekOf);
    }
    return this.http.post<WeeklyBrief>('/api/briefing/weekly/refresh', {}, { params });
  }
}
