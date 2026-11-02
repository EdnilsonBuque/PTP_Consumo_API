# Consumo_API_CSharp

Aplicação de consola em C# com menus interativos para consumir duas APIs REST. As respostas JSON são convertidas em objetos C# com `System.Text.Json`.

## Requisitos

- .NET 10 SDK
- Ligação à Internet

## Executar

Abra `Consumo_API_CSharp.sln` no Visual Studio e inicie com **F5** ou **Ctrl+F5**. A solução já aponta para o projeto dentro da pasta `Consumo_API_CSharp`.

Também pode executar pela pasta raiz do repositório:

```powershell
dotnet run --project .\Consumo_API_CSharp\Consumo_API_CSharp.csproj
```

No menu principal, escolha uma opção:


1. **Bíblia:** texto de João 3:16, através da [Bible API](https://bible-api.com/john%203:16).
2. **Piada do dia:** piada em francês, através da API indicada na página [French Joke API](https://freepublicapis.com/french-joke-api). O programa também mostra a atribuição exigida pelo fornecedor.

O programa usa pedidos HTTP `GET`, valida as opções do menu, repete até três vezes erros temporários de servidor (como HTTP 503) com espera progressiva e apresenta mensagens explícitas para falhas de rede, timeout e respostas JSON inválidas. Se uma API continuar indisponível, será necessário tentar novamente mais tarde. O catálogo atual em português da JokeAPI é pequeno (duas piadas) e está classificado pela API na categoria geral `Misc`; os temas selecionam as piadas disponíveis no catálogo.

