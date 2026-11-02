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

1. **Meteorologia:** temperatura e velocidade do vento atuais para Berlim, através do [Open-Meteo](https://api.open-meteo.com/v1/forecast?latitude=52.52&longitude=13.41&current=temperature_2m,wind_speed_10m).
2. **Bíblia:** texto de João 3:16, através da [Bible API](https://bible-api.com/john%203:16).
3. **Piada do dia:** piada em francês, através da API indicada na página [French Joke API](https://freepublicapis.com/french-joke-api). O programa também mostra a atribuição exigida pelo fornecedor.

O programa usa pedidos HTTP `GET` e apresenta mensagens explícitas para falhas de rede, timeout e respostas JSON inválidas.

## Publicar no GitHub

Crie no GitHub um repositório público com o nome exato `PTP_Consumo_API` e clone-o para uma pasta própria:

```powershell
git clone https://github.com/<utilizador>/PTP_Consumo_API.git
```

Copie os ficheiros deste projeto para a pasta clonada. Execute os comandos seguintes **dentro dessa pasta clonada**, para garantir que o envio é feito para o repositório correto:

```powershell
git add .
git commit -m "Adicionar projeto de consumo de API em C#"
git push -u origin main
```

Submeta o URL do repositório público no Google Classroom.
