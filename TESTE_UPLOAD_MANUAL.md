# Teste Manual do Upload de Documentos

## Problema Atual
O erro "Failed to fetch" indica que a aplicação não está conseguindo se comunicar com o servidor.

## Passos para Testar

### 1. Iniciar a Aplicação
Abra um terminal e execute:
```bash
cd WebAppSystems
dotnet run
```

Aguarde até ver a mensagem:
```
Now listening on: http://localhost:8000
Now listening on: https://localhost:5095
```

### 2. Acessar a Tela de Análise
Abra o navegador e acesse:
```
http://localhost:8000/DocumentAnalysis
```

### 3. Fazer Upload de um Documento
1. Clique na área de upload ou arraste um arquivo
2. Selecione um arquivo PDF, DOCX ou TXT (máximo 10 MB)
3. (Opcional) Selecione um cliente
4. Clique em "Analisar Documento"

### 4. Verificar o Resultado
- Se funcionar: Você será redirecionado para a página de detalhes com a análise completa
- Se der erro: Anote a mensagem de erro e me informe

## Verificar Logs
Se der erro, verifique os logs no terminal onde a aplicação está rodando. Procure por:
- Mensagens de erro em vermelho
- Status HTTP 404, 500, etc.
- Mensagens sobre a API do Google Gemini

## Possíveis Problemas

### Erro "Failed to fetch"
- **Causa**: Aplicação não está rodando
- **Solução**: Certifique-se de que `dotnet run` está executando

### Erro "API Key não configurada"
- **Causa**: API Key do Google não está no appsettings.json
- **Solução**: Já está configurada, não deve acontecer

### Erro 404 da API do Google
- **Causa**: URL da API estava incorreta (já corrigida)
- **Solução**: Já foi corrigida para usar v1beta

### Documento fica "Pending" ou "Processing"
- **Causa**: Análise não está sendo executada
- **Solução**: Já foi corrigida para executar de forma síncrona

## Teste com Arquivo Pequeno
Para o primeiro teste, use um arquivo TXT pequeno com este conteúdo:

```
PETIÇÃO INICIAL - AÇÃO TRABALHISTA

Exmo. Sr. Dr. Juiz de Direito da Vara do Trabalho

JOÃO DA SILVA, brasileiro, solteiro, operário, portador da CTPS nº 12345, 
residente e domiciliado na Rua das Flores, 123, São Paulo/SP, vem, 
respeitosamente, à presença de Vossa Excelência, por meio de seu advogado 
que esta subscreve, propor a presente

RECLAMAÇÃO TRABALHISTA

em face de EMPRESA XYZ LTDA, pessoa jurídica de direito privado, inscrita 
no CNPJ sob o nº 12.345.678/0001-90, com sede na Av. Paulista, 1000, 
São Paulo/SP, pelos fatos e fundamentos jurídicos a seguir expostos:

DOS FATOS

O Reclamante foi admitido pela Reclamada em 01/01/2020, para exercer a 
função de operário, com jornada de trabalho de 8 horas diárias.

Durante todo o período contratual, o Reclamante laborou em condições 
insalubres, sem o devido pagamento do adicional de insalubridade.

Em 31/12/2023, o Reclamante foi dispensado sem justa causa.

DO DIREITO

O artigo 192 da CLT estabelece o direito ao adicional de insalubridade.

PEDIDOS

a) Pagamento de adicional de insalubridade no percentual de 40%;
b) Diferenças de FGTS e multa de 40%;
c) Aviso prévio indenizado;
d) 13º salário proporcional;
e) Férias proporcionais acrescidas de 1/3.

Valor da causa: R$ 50.000,00

Termos em que,
Pede deferimento.

São Paulo, 02 de março de 2026.

Advogado OAB/SP 123456
```

Salve este texto em um arquivo chamado `teste.txt` e faça o upload.

## O Que Esperar
Se tudo funcionar corretamente, você verá:
1. Barra de progresso "Analisando documento com IA..."
2. Redirecionamento automático para a página de detalhes
3. Análise completa com:
   - Resumo executivo
   - Área do direito (Trabalhista)
   - Tipo de ação
   - Complexidade
   - Horas estimadas
   - Tópicos principais
   - Base legal
   - Partes envolvidas
   - Valor da causa
   - Prazos
   - Advogados recomendados

## Se Não Funcionar
Me informe:
1. A mensagem de erro exata
2. Os logs do terminal
3. Em que etapa parou (upload, análise, etc.)
