# Controle de Estoque — Cliente Desktop (WPF)

Aplicativo Windows para a **Controle de Estoque API**. Feito em **WPF + MVVM** (CommunityToolkit.Mvvm). O menu e as telas mudam conforme o perfil do usuário, seguindo as permissões da API.

> ⚠️ **Este código não foi compilado nem executado** (o ambiente onde ele foi escrito não tem o .NET SDK). Rode `dotnet build` e corrija os eventuais ajustes pequenos de compilação antes de usar.

## Telas por perfil

| Tela | Caixa | Estoquista | Gerente |
|---|:---:|:---:|:---:|
| Vender (PDV com carrinho) | ✅ | ❌ | ✅ |
| Produtos (CRUD, desativar) | ❌ | ✅ | ✅ |
| Estoque (entrada, saída avulsa, histórico) | ❌ | ✅ | ✅ |
| Categorias | ❌ | ✅ | ✅ |
| Fornecedores | ❌ | ❌ | ✅ |
| Vendas (histórico com itens) | ❌ | ❌ | ✅ |
| Usuários | ❌ | ❌ | ✅ |
| Minha conta (trocar senha) | ✅ | ✅ | ✅ |


### 1. Rode a API

Anote o endereço HTTPS que ela usa (veja `Properties/launchSettings.json`, por exemplo `https://localhost:7096`). Se o navegador reclamar do certificado, rode uma vez:

```
dotnet dev-certs https --trust
```

### 2. Rode o app

Abra o `ControleEstoque.Desktop.csproj` no Visual Studio (ou rode `dotnet run` na pasta do projeto). Na tela de login, confira o **Endereço da API** e entre com o seu usuário. O endereço padrão vem do `appsettings.json` deste projeto.

Se você usa o .NET 8 ou 9, troque `net10.0-windows` por `net8.0-windows` ou `net9.0-windows` no `.csproj`.

## Estrutura

```
ControleEstoque.Desktop/
├── App.xaml(.cs)          # inicialização, cultura pt-BR, login ↔ janela principal
├── Helpers/               # conversores, PasswordBox com binding, diálogo de confirmação
├── Models/Dtos.cs         # formato das respostas da API
├── Services/
│   ├── ApiClient.cs       # HttpClient + token JWT + tradução de erros (400/401/403/404/409/429)
│   ├── Sessao.cs          # usuário logado (token só em memória)
│   └── CatalogoService.cs # carrega todas as páginas de produtos
├── ViewModels/            # uma por tela + Paginacao reutilizável
├── Views/                 # LoginWindow e MainWindow
└── Resources/
    ├── Styles.xaml        # cores e estilos
    └── Templates.xaml     # uma tela (DataTemplate) para cada ViewModel
```

## Comportamentos importantes

- **Sessão:** se a API responder 401 (token expirado, usuário desativado), o app volta para o login.
- **Banco no Azure gratuito:** depois de ficar parado, o banco pausa. A primeira chamada pode falhar com "Erro interno"; tente de novo depois de cerca de 1 minuto.
- **Venda:** usuário e data são definidos pela API; o app envia apenas os itens e a forma de pagamento.
- **Filtro de produtos:** filtra a página exibida (a API não tem busca por nome).
- **Histórico de estoque:** por padrão mostra os últimos 30 dias; escolher um produto ignora as datas.
