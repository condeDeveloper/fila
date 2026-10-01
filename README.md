# fila

Monte binário e fila de prioridade do zero em C# e .NET 8, com a invariante
conferida depois de cada operação.

```
$ dotnet medidor.dll kmenores

           n      k    monte de k   ordenar tudo     razao
  --------------------------------------------------------------
   1,000,000     10         6.0 ms        58.3 ms      9.7x
  10,000,000     10        22.6 ms       709.7 ms     31.3x

  Os 10 menores de um fluxo de 10 milhoes, sem nunca materializar:
    53, 141, 248, 513, 665, 982, 989, 1249, 1404, 2218
    em 171 ms, com 10 itens na memoria
```

## Dois juízes, e o segundo é o que importa

O primeiro é o `PriorityQueue` do .NET, em **um milhão de operações sorteadas**
intercalando inserção e remoção. Ele chegou no .NET 6 e é, por baixo, um monte
quaternário, então é a mesma estrutura escrita por quem mantém a plataforma.

O segundo é a **invariante conferida depois de cada operação**, e vale explicar
por quê: um monte com a invariante quebrada **continua respondendo**. Ele devolve
itens, devolve a quantidade certa, e só a ordem está errada. A comparação de saída
pega o erro quando ele aparece; a invariante pega a hora em que ele nasce.

São 18 testes.

## A invariante é fraca de propósito

Todo pai é menor ou igual aos filhos, e nada mais. **Um monte não é ordenado**, e
isso é o ponto: manter a ordem total custaria `O(n)` por inserção, e manter só a
relação pai-filho custa `O(log n)` com o menor sempre na posição 0.

E a árvore mora num vetor sem ponteiro nenhum: o filho esquerdo de `i` é `2i+1`, o
direito é `2i+2`, o pai é `(i−1)/2`. Não há nó, não há alocação por elemento, e a
árvore é sempre completa porque o vetor não tem buracos.

```
$ dotnet medidor.dll altura

           n   altura   comparacoes no pior caso de uma remocao
  ------------------------------------------------------------------
           1        1            0
       1,000       10           18
   1,000,000       20           38
 1,000,000,000     30           58
```

A altura é exatamente `floor(log2(n)) + 1`, sem exceção, e não há caso degenerado
como numa árvore de busca. Um bilhão de itens tem altura 30.

## Montar de uma vez é `O(n)`, e a conta não é óbvia

```
$ dotnet medidor.dll montar

        n    de uma vez    um a um     razao
  ------------------------------------------------------
  100,000        1.5 ms      4.8 ms      3.2x
1,000,000       14.2 ms     46.1 ms      3.2x
5,000,000       79.3 ms    252.0 ms      3.2x
```

Parece que descer `n/2` nós deveria custar `n/2 · log n`. A conta certa é outra:
metade dos nós são folhas e não descem nada, um quarto desce no máximo um nível,
um oitavo desce dois. A soma `n/4·1 + n/8·2 + n/16·3 + …` converge para `n`, e não
para `n·log n`.

## Os k menores sem ordenar os n

Achar os 10 maiores de um bilhão de itens não exige ordenar um bilhão de itens.
Basta um monte de tamanho 10 com o **comparador invertido**, para que o topo seja
o pior dos guardados: assim a pergunta "este item entra?" é uma olhada na raiz, em
`O(1)`.

O custo cai de `O(n·log n)` para `O(n·log k)`. Mas a vantagem de tempo não é o
ponto: o ponto é a **memória**, que cai de `O(n)` para `O(k)`. Com `k` pequeno, o
algoritmo roda sobre um fluxo que não cabe na memória, e um dos testes confere
isso passando uma sequência preguiçosa de dez milhões de itens que nunca vira
vetor. Se o algoritmo materializasse a entrada, o teste estouraria em vez de
passar.

## A intercalação lê exatamente o necessário

```
$ dotnet medidor.dll intercalar

  Pedindo so os 5 primeiros de 3 sequencias de mil itens:

    saida            0, 1, 2, 3, 4
    leituras da 1a   5
    leituras da 2a   1
    leituras da 3a   1
```

As sequências 2 e 3 contribuíram com **um item cada**, que é o que está no monte
esperando a vez. É o que permite ordenar um arquivo maior que a memória: a
intercalação guarda uma linha de cada bloco, então a memória é `O(k)`.

Eu previ seis leituras da primeira sequência e são cinco. A conta certa: uma
leitura inicial para pôr no monte, e depois uma reposição após cada entrega, só
que o `Take(5)` para no quinto item e **nunca pede o sexto**. A preguiça é ainda
mais apertada do que eu supus.

## A estabilidade não vem de graça

Um monte troca itens distantes entre si, então dois itens de mesma prioridade saem
em ordem arbitrária. O `PriorityQueue` do .NET documenta isso explicitamente.

A saída é um contador de entrada que serve de desempate, e aqui ela é **opcional**
porque custa oito bytes por item e uma comparação a mais. Um teste mede as duas:
a fila estável nunca troca, e a comum troca em algum dos 2.000 casos. Se a comum
nunca trocasse, o teste falharia dizendo que ou os empates não foram exercitados
ou ela virou estável por acidente.

## Contra o `PriorityQueue`

```
$ dotnet medidor.dll velocidade

           n     por e tirar aqui   PriorityQueue    razao
  --------------------------------------------------------------
     100,000              7.9 ms          5.1 ms     1.55x
   1,000,000             95.3 ms         61.4 ms     1.55x
   5,000,000            561.7 ms        353.1 ms     1.59x
```

O `PriorityQueue` é um monte **quaternário**: cada nó tem quatro filhos, então a
árvore fica mais rasa (`log4(n)` níveis) ao preço de mais comparações por nível na
descida. E ele guarda item e chave num struct e evita a chamada virtual do
`IComparer` no caso comum, que é o que mais custa num laço apertado.

## Rodar

.NET 8. Zero dependências fora do xUnit, e só nos testes.

```
dotnet test testes/Fila.Testes/Fila.Testes.csproj -c Release
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- kmenores
```

As medidas de tempo moram na ferramenta e não na suíte, de propósito: uma
afirmação de relógio falha sem que nada tenha piorado, basta a máquina do CI estar
ocupada.

## O que ele não faz

Não tem **diminuir chave**, que é o que o Dijkstra realmente quer: achar um item
no meio do monte exige um índice de posição por item, e manter esse índice
atualizado a cada troca. Sem ele, a saída é enfileirar o mesmo vértice de novo e
descartar as cópias antigas na saída, que é o que a maioria das implementações
faz.

Não tem monte de Fibonacci nem monte pareado, que têm inserção amortizada em
`O(1)` e são melhores na teoria. Na prática eles perdem para o binário em quase
tudo, porque a constante e as faltas de cache comem a vantagem.

Não é thread-safe, e não é um monte quaternário como o do .NET.

Não tem monte duplo, que responde o menor e o maior ao mesmo tempo.

## Licença

MIT.
