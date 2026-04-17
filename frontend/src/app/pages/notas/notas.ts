import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

import { Nota } from '../../models/nota';
import { Produto } from '../../models/produto';
import { NotasService } from '../../services/notas';
import { ProdutosService } from '../../services/produtos';

type ItemNotaFormulario = {
  productId: number;
  productCode: string;
  productDescription: string;
  quantity: number;
};

@Component({
  selector: 'app-notas',
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
  templateUrl: './notas.html',
  styleUrl: './notas.css'
})
export class Notas implements OnInit {
  private notasService = inject(NotasService);
  private produtosService = inject(ProdutosService);
  private fb = inject(FormBuilder);
  private cdr = inject(ChangeDetectorRef);
  private snackBar = inject(MatSnackBar);

  notas: Nota[] = [];
  produtos: Produto[] = [];
  itensNota: ItemNotaFormulario[] = [];

  displayedColumns: string[] = ['id', 'number', 'status', 'createdAt', 'items', 'actions'];

  carregando = false;
  carregandoProdutos = false;
  salvando = false;
  imprimindoId: number | null = null;
  excluindoId: number | null = null;
  notaEmEdicaoId: number | null = null;
  mensagemErro = '';
  tentouAdicionarItem = false;

  form = this.fb.group({
    productId: [null as number | null, Validators.required],
    quantity: [1, [Validators.required, Validators.min(1)]]
  });

  ngOnInit(): void {
    this.carregarProdutos();
    this.carregarNotas();
  }

  get productIdControl() {
    return this.form.controls.productId;
  }

  get quantityControl() {
    return this.form.controls.quantity;
  }

  carregarProdutos(): void {
    this.carregandoProdutos = true;
    this.cdr.detectChanges();

    this.produtosService.listar().subscribe({
      next: (dados) => {
        this.produtos = [...dados];
        this.carregandoProdutos = false;
        this.cdr.detectChanges();
      },
      error: (erro) => {
        console.error('Erro ao carregar produtos', erro);
        this.mensagemErro = 'Não conseguimos carregar os produtos para montar a nota.';
        this.carregandoProdutos = false;
        this.cdr.detectChanges();
      }
    });
  }

  carregarNotas(): void {
    this.carregando = true;
    this.mensagemErro = '';
    this.cdr.detectChanges();

    this.notasService.listar().subscribe({
      next: (dados) => {
        this.notas = [...dados];
        this.carregando = false;
        this.cdr.detectChanges();
      },
      error: (erro) => {
        console.error('Erro ao carregar notas', erro);
        this.mensagemErro = 'Não conseguimos carregar as notas.';
        this.carregando = false;
        this.cdr.detectChanges();
      }
    });
  }

  adicionarItem(): void {
    this.tentouAdicionarItem = true;

    if (this.form.invalid) {
      this.mensagemErro = 'Selecione um produto e informe uma quantidade válida.';
      this.cdr.detectChanges();
      return;
    }

    const valor = this.form.getRawValue();
    const productId = Number(valor.productId);
    const quantity = Number(valor.quantity);

    const produto = this.produtos.find((item) => item.id === productId);

    if (!produto) {
      this.mensagemErro = 'Selecione um produto válido.';
      this.cdr.detectChanges();
      return;
    }

    const itemExistente = this.itensNota.find((item) => item.productId === productId);

    if (itemExistente) {
      itemExistente.quantity += quantity;
      this.itensNota = [...this.itensNota];
    } else {
      this.itensNota = [
        ...this.itensNota,
        {
          productId: produto.id,
          productCode: produto.code,
          productDescription: produto.description,
          quantity
        }
      ];
    }

    this.mensagemErro = '';
    this.tentouAdicionarItem = false;
    this.form.reset({
      productId: null,
      quantity: 1
    });
    this.form.markAsPristine();
    this.form.markAsUntouched();
    this.cdr.detectChanges();
  }

  removerItem(productId: number): void {
    this.itensNota = this.itensNota.filter((item) => item.productId !== productId);
    this.cdr.detectChanges();
  }

  iniciarEdicao(nota: Nota): void {
    if (nota.status !== 'Aberta') {
      return;
    }

    this.notaEmEdicaoId = nota.id;
    this.itensNota = nota.items.map((item) => ({
      productId: item.productId,
      productCode: item.productCode,
      productDescription: item.productDescription,
      quantity: item.quantity
    }));

    this.form.reset({
      productId: null,
      quantity: 1
    });

    this.tentouAdicionarItem = false;
    this.form.markAsPristine();
    this.form.markAsUntouched();
    this.mensagemErro = '';
    this.cdr.detectChanges();
  }

  cancelarEdicao(): void {
    this.notaEmEdicaoId = null;
    this.itensNota = [];
    this.mensagemErro = '';
    this.tentouAdicionarItem = false;

    this.form.reset({
      productId: null,
      quantity: 1
    });

    this.form.markAsPristine();
    this.form.markAsUntouched();
    this.cdr.detectChanges();
  }

  salvar(): void {
    if (this.salvando) {
      return;
    }

    if (this.itensNota.length === 0) {
      this.mensagemErro = 'Adicione pelo menos um item antes de salvar a nota.';
      this.cdr.detectChanges();
      return;
    }

    this.salvando = true;
    this.mensagemErro = '';
    this.cdr.detectChanges();

    const dados = {
      items: this.itensNota.map((item) => ({
        productId: item.productId,
        productCode: item.productCode,
        productDescription: item.productDescription,
        quantity: item.quantity
      }))
    };

    const requisicao = this.notaEmEdicaoId
      ? this.notasService.atualizar(this.notaEmEdicaoId, dados)
      : this.notasService.criar(dados);

    requisicao.subscribe({
      next: () => {
        const mensagemSucesso = this.notaEmEdicaoId
          ? 'Nota atualizada com sucesso.'
          : 'Nota cadastrada com sucesso.';

        this.salvando = false;
        this.cancelarEdicao();
        this.exibirSucesso(mensagemSucesso);
        this.cdr.detectChanges();
        this.carregarNotas();
      },
      error: (erro) => {
        console.error('Erro ao salvar nota', erro);
        this.mensagemErro = this.extrairMensagemErro(
          erro,
          this.notaEmEdicaoId
            ? 'Não conseguimos atualizar a nota.'
            : 'Não conseguimos salvar a nota.'
        );
        this.salvando = false;
        this.cdr.detectChanges();
      }
    });
  }

  excluir(nota: Nota): void {
    if (nota.status !== 'Aberta' || this.excluindoId !== null) {
      return;
    }

    const confirmado = window.confirm(`Deseja excluir a nota ${nota.number}?`);

    if (!confirmado) {
      return;
    }

    this.excluindoId = nota.id;
    this.mensagemErro = '';
    this.cdr.detectChanges();

    this.notasService.excluir(nota.id).subscribe({
      next: () => {
        if (this.notaEmEdicaoId === nota.id) {
          this.cancelarEdicao();
        }

        this.excluindoId = null;
        this.exibirSucesso('Nota excluída com sucesso.');
        this.cdr.detectChanges();
        this.carregarNotas();
      },
      error: (erro) => {
        console.error('Erro ao excluir nota', erro);
        this.mensagemErro = this.extrairMensagemErro(erro, 'Não conseguimos excluir a nota.');
        this.excluindoId = null;
        this.cdr.detectChanges();
      }
    });
  }

  podeEditarOuExcluir(nota: Nota): boolean {
    return nota.status === 'Aberta';
  }

  podeImprimir(nota: Nota): boolean {
    return nota.status === 'Aberta' && this.imprimindoId !== nota.id && this.excluindoId !== nota.id;
  }

  imprimir(nota: Nota): void {
    if (!this.podeImprimir(nota)) {
      return;
    }

    const idempotencyKey = this.notasService.gerarIdempotencyKey(nota.id);
    const startedAt = Date.now();
    const tempoMinimoMs = 900;

    this.imprimindoId = nota.id;
    this.mensagemErro = '';
    this.cdr.detectChanges();

    this.notasService.imprimir(nota.id, idempotencyKey).subscribe({
      next: () => {
        this.finalizarFluxoImpressao(startedAt, tempoMinimoMs, () => {
          if (this.notaEmEdicaoId === nota.id) {
            this.cancelarEdicao();
          }

          this.imprimindoId = null;
          this.exibirSucesso('Nota impressa com sucesso.');
          this.cdr.detectChanges();
          this.carregarNotas();
          this.carregarProdutos();
        });
      },
      error: (erro) => {
        console.error('Erro ao imprimir nota', erro);

        this.finalizarFluxoImpressao(startedAt, tempoMinimoMs, () => {
          this.imprimindoId = null;
          this.mensagemErro = this.extrairMensagemErro(erro, 'Não conseguimos concluir a impressão da nota.');
          this.cdr.detectChanges();
        });
      }
    });
  }

  itensVisiveisNota(nota: Nota): ItemNotaFormulario[] {
    return nota.items.map((item) => ({
      productId: item.productId,
      productCode: item.productCode,
      productDescription: item.productDescription,
      quantity: item.quantity
    }));
  }

  obterLabelProduto(produto: Produto): string {
    return `${produto.code} - ${produto.description} | Saldo disponível: ${produto.stock}`;
  }

  exibirErroProduto(): string {
    if (!this.tentouAdicionarItem) {
      return '';
    }

    if (this.productIdControl.hasError('required')) {
      return 'Selecione um produto.';
    }

    return '';
  }

  exibirErroQuantidade(): string {
    if (!this.tentouAdicionarItem) {
      return '';
    }

    if (this.quantityControl.hasError('required')) {
      return 'Informe a quantidade.';
    }

    if (this.quantityControl.hasError('min')) {
      return 'A quantidade deve ser maior que zero.';
    }

    return '';
  }

  private finalizarFluxoImpressao(inicio: number, tempoMinimoMs: number, callback: () => void): void {
    const elapsed = Date.now() - inicio;
    const restante = Math.max(tempoMinimoMs - elapsed, 0);

    setTimeout(() => {
      callback();
    }, restante);
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
    const bruto =
      erro?.error?.details ??
      erro?.error?.message ??
      erro?.error ??
      '';

    const normalizado = this.normalizarMensagem(bruto);

    return normalizado ? `${mensagemPadrao} ${normalizado}` : mensagemPadrao;
  }

  private normalizarMensagem(valor: unknown): string {
    if (!valor) {
      return '';
    }

    if (typeof valor === 'string') {
      const texto = valor.trim();

      if ((texto.startsWith('{') && texto.endsWith('}')) || (texto.startsWith('[') && texto.endsWith(']'))) {
        try {
          const json = JSON.parse(texto);
          return this.normalizarMensagem(json);
        } catch {
          return texto;
        }
      }

      return texto;
    }

    if (typeof valor === 'object') {
      const obj = valor as Record<string, unknown>;

      const details = this.normalizarMensagem(obj['details']);
      if (details) {
        return details;
      }

      const message = this.normalizarMensagem(obj['message']);
      if (message) {
        return message;
      }

      const values = Object.values(obj)
        .map((item) => this.normalizarMensagem(item))
        .filter((item) => !!item);

      return values.join(' ');
    }

    return String(valor);
  }
}
