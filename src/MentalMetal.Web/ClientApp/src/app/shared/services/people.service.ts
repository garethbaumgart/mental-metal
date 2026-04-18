import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CreatePersonRequest,
  Person,
  PersonType,
  UpdatePersonRequest,
} from '../models/person.model';

@Injectable({ providedIn: 'root' })
export class PeopleService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/people';

  list(type?: PersonType, includeArchived = false): Observable<Person[]> {
    let params = new HttpParams();
    if (type) {
      params = params.set('type', type);
    }
    if (includeArchived) {
      params = params.set('includeArchived', 'true');
    }
    return this.http.get<Person[]>(this.baseUrl, { params });
  }

  get(id: string): Observable<Person> {
    return this.http.get<Person>(`${this.baseUrl}/${id}`);
  }

  create(request: CreatePersonRequest): Observable<Person> {
    return this.http.post<Person>(this.baseUrl, request);
  }

  update(id: string, request: UpdatePersonRequest): Observable<Person> {
    return this.http.put<Person>(`${this.baseUrl}/${id}`, request);
  }

  changeType(id: string, newType: PersonType): Observable<Person> {
    return this.http.put<Person>(`${this.baseUrl}/${id}/type`, { newType });
  }

  setAliases(id: string, aliases: string[]): Observable<Person> {
    return this.http.put<Person>(`${this.baseUrl}/${id}/aliases`, { aliases });
  }

  addAlias(id: string, alias: string): Observable<Person> {
    return this.http.post<Person>(`${this.baseUrl}/${id}/aliases`, { alias });
  }

  archive(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/archive`, {});
  }
}
