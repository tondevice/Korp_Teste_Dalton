import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Produto } from '../models/produto';

export interface ProdutoCreateDto {
  code: string;
  description: string;
  stock: number;
}

@Injectable({
  providedIn: 'root'
})
export class ProdutosService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5119/api/produtos';

  listar(): Observable<Produto[]> {
    return this.http.get<Produto[]>(this.apiUrl);
  }

  criar(dados: ProdutoCreateDto): Observable<Produto> {
    return this.http.post<Produto>(this.apiUrl, dados);
  }

  atualizar(id: number, dados: ProdutoCreateDto): Observable<Produto> {
    return this.http.put<Produto>(`${this.apiUrl}/${id}`, dados);
  }

  excluir(id: number): Observable<unknown> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}