# Consumo_API_CSharp

Aplicação de consola em C# que permite escolher e consumir três APIs REST. As respostas JSON são convertidas em objetos C# com `System.Text.Json`.

## Requisitos

- .NET 10 SDK
- Ligação à Internet

## Executar

Na pasta do projeto, execute:

```powershell
dotnet run
```

No menu, escolha uma opção:


1. **Bíblia:** texto de João 3:16, através da [Bible API](https://bible-api.com/john%203:16).
2. **Piada do dia:** piada em francês, através da API indicada na página [French Joke API](https://freepublicapis.com/french-joke-api). O programa também mostra a atribuição exigida pelo fornecedor.

O programa usa pedidos HTTP `GET` e apresenta mensagens explícitas para falhas de rede, timeout e respostas JSON inválidas.

