export interface ItemNota {
  id: number;
  invoiceId: number;
  productId: number;
  productCode: string;
  productDescription: string;
  quantity: number;
}

export interface Nota {
  id: number;
  number: number;
  status: string;
  createdAt: string;
  items: ItemNota[];
}