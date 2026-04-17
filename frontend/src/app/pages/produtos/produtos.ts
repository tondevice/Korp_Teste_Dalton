import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

import { Produto } from '../../models/produto';
import { ProdutosService } from '../../services/produtos';

@Component({
  selector: 'app-produtos',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatTableModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSnackBarModule
  ],
  templateUrl: './produtos.html',
  styleUrl: './produtos.css'
})
export class Produtos implements OnInit {
  private produtosService = inject(ProdutosService);
  private fb = inject(FormBuilder);
  private cdr = inject(ChangeDetectorRef);
  private snackBar = inject(MatSnackBar);

  produtos: Produto[] = [];
  displayedColumns: string[] = ['id', 'code', 'description', 'stock', 'actions'];

  carregando = false;
  salvando = false;
  excluindoId: number | null = null;
  produtoEmEdicaoId: number | null = null;
  mensagemErro = '';
  tentouSalvar = false;

  form = this.fb.group({
    code: ['', Validators.required],
    description: ['', Validators.required],
    stock: [0, [Validators.required, Validators.min(0)]]
  });

  ngOnInit(): void {
    this.carregarProdutos();
  }

  get codeControl() {
    return this.form.controls.code;
  }

  get descriptionControl() {
    return this.form.controls.description;
  }

  get stockControl() {
    return this.form.controls.stock;
  }

  carregarProdutos(): void {
    this.carregando = true;
    this.mensagemErro = '';
    this.cdr.detectChanges();

    this.produtosService.listar().subscribe({
      next: (dados) => {
        this.produtos = [...dados];
        this.carregando = false;
        this.cdr.detectChanges();
      },
      error: (erro) => {
        console.error('Erro ao carregar produtos', erro);
        this.mensagemErro = this.extrairMensagemErro(erro, 'Não conseguimos carregar os produtos.');
        this.carregando = false;
        this.cdr.detectChanges();
      }
    });
  }

  iniciarEdicao(produto: Produto): void {
    this.produtoEmEdicaoId = produto.id;
    this.mensagemErro = '';
    this.tentouSalvar = false;

    this.form.reset({
      code: produto.code,
      description: produto.description,
      stock: produto.stock
    });

    this.form.markAsPristine();
    this.form.markAsUntouched();
    this.cdr.detectChanges();
  }

  cancelarEdicao(): void {
    this.produtoEmEdicaoId = null;
    this.mensagemErro = '';
    this.tentouSalvar = false;

    this.form.reset({
      code: '',
      description: '',
      stock: 0
    });

    this.form.markAsPristine();
    this.form.markAsUntouched();
    this.cdr.detectChanges();
  }

  salvar(): void {
    this.tentouSalvar = true;

    if (this.form.invalid || this.salvando) {
      this.mensagemErro = 'Confira os campos obrigatórios antes de continuar.';
      this.cdr.detectChanges();
      return;
    }

    this.salvando = true;
    this.mensagemErro = '';
    this.cdr.detectChanges();

    const dados = this.form.getRawValue() as {
      code: string;
      description: string;
      stock: number;
    };

    const payload = {
      code: dados.code.trim(),
      description: dados.description.trim(),
      stock: Number(dados.stock)
    };

    const requisicao = this.produtoEmEdicaoId
      ? this.produtosService.atualizar(this.produtoEmEdicaoId, payload)
      : this.produtosService.criar(payload);

    requisicao.subscribe({
      next: () => {
        const mensagemSucesso = this.produtoEmEdicaoId
          ? 'Produto atualizado com sucesso.'
          : 'Produto cadastrado com sucesso.';

        this.salvando = false;
        this.cancelarEdicao();
        this.exibirSucesso(mensagemSucesso);
        this.carregarProdutos();
      },
      error: (erro) => {
        console.error('Erro ao salvar produto', erro);
        this.mensagemErro = this.extrairMensagemErro(
          erro,
          this.produtoEmEdicaoId
            ? 'Não conseguimos atualizar o produto.'
            : 'Não conseguimos salvar o produto.'
        );
        this.salvando = false;
        this.cdr.detectChanges();
      }
    });
  }

  excluir(produto: Produto): void {
    if (this.excluindoId !== null) {
      return;
    }

    const confirmado = window.confirm(`Deseja excluir o produto ${produto.code} - ${produto.description}?`);

    if (!confirmado) {
      return;
    }

    this.excluindoId = produto.id;
    this.mensagemErro = '';
    this.cdr.detectChanges();

    this.produtosService.excluir(produto.id).subscribe({
      next: () => {
        if (this.produtoEmEdicaoId === produto.id) {
          this.cancelarEdicao();
        }

        this.excluindoId = null;
        this.exibirSucesso('Produto excluído com sucesso.');
        this.cdr.detectChanges();
        this.carregarProdutos();
      },
      error: (erro) => {
        console.error('Erro ao excluir produto', erro);
        this.mensagemErro = this.extrairMensagemErro(erro, 'Não conseguimos excluir o produto.');
        this.excluindoId = null;
        this.cdr.detectChanges();
      }
    });
  }

  exibirErroCodigo(): string {
    if (!this.tentouSalvar) {
      return '';
    }

    if (this.codeControl.hasError('required')) {
      return 'Preencha o código do produto.';
    }

    return '';
  }

  exibirErroDescricao(): string {
    if (!this.tentouSalvar) {
      return '';
    }

    if (this.descriptionControl.hasError('required')) {
      return 'Preencha a descrição do produto.';
    }

    return '';
  }

  exibirErroSaldo(): string {
    if (!this.tentouSalvar) {
      return '';
    }

    if (this.stockControl.hasError('required')) {
      return 'Informe o saldo do produto.';
    }

    if (this.stockControl.hasError('min')) {
      return 'O saldo deve ser zero ou maior.';
    }

    return '';
  }

  private exibirSucesso(mensagem: string): void {
    this.snackBar.open(mensagem, 'Fechar', {
      duration: 3000,
      horizontalPosition: 'center',
      verticalPosition: 'bottom',
      panelClass: ['success-snackbar']
    });
  }

  private extrairMensagemErro(erro: any, mensagemPadrao: string): string {
    const detalhe =
      erro?.error?.details ||
      erro?.error?.message ||
      (typeof erro?.error === 'string' ? erro.error : '');

    return detalhe ? `${mensagemPadrao} ${detalhe}` : mensagemPadrao;
  }
}
