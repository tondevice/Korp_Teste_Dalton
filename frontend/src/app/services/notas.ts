import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Nota } from '../models/nota';

export interface ItemNotaCreateDto {
  productId: number;
  productCode: string;
  productDescription: string;
  quantity: number;
}

export interface NotaCreateDto {
  items: ItemNotaCreateDto[];
}

@Injectable({
  providedIn: 'root'
})
export class NotasService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5133/api/notas';

  listar(): Observable<Nota[]> {
    return this.http.get<Nota[]>(this.apiUrl);
  }

  criar(dados: NotaCreateDto): Observable<Nota> {
    return this.http.post<Nota>(this.apiUrl, dados);
  }

  atualizar(id: number, dados: NotaCreateDto): Observable<Nota> {
    return this.http.put<Nota>(`${this.apiUrl}/${id}`, dados);
  }

  excluir(id: number): Observable<unknown> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  imprimir(id: number, idempotencyKey: string): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/${id}/impressao`, null, {
      headers: new HttpHeaders({
        'Idempotency-Key': idempotencyKey
      })
    });
  }

  gerarIdempotencyKey(id: number): string {
    if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
      return `print-note-${id}-${crypto.randomUUID()}`;
    }

    return `print-note-${id}-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
  }
}