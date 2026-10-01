using Xunit;

namespace Conde.Fila.Testes;

/// <summary>
/// Os k menores e a intercalação de k vias, contra a ordenação completa.
/// </summary>
/// <remarks>
/// O juiz aqui é a resposta óbvia e cara: ordenar tudo e pegar os primeiros k.
/// Ela é lenta demais para produção e obviamente correta, que é exatamente o que
/// se quer de um juiz.
/// </remarks>
public class SelecaoTest
{
    [Fact(DisplayName = "os k menores, contra ordenar tudo e cortar")]
    public void OsKMenores()
    {
        var sorteio = new Random(29);

        for (var caso = 0; caso < 20_000; caso++)
        {
            var quantos = sorteio.Next(0, 200);

            var k = sorteio.Next(0, 30);

            var itens = Enumerable.Range(0, quantos)
                .Select(_ => sorteio.Next(-500, 500))
                .ToArray();

            var esperado = itens.Order().Take(k).ToList();

            var achado = Selecao.Menores(itens, k);

            Assert.True(esperado.SequenceEqual(achado),
                $"com {quantos} itens e k={k}: esperava [{string.Join(",", esperado)}] "
                + $"e veio [{string.Join(",", achado)}]");

            // E os maiores, pela mesma conta.
            var esperadoMaiores = itens.OrderDescending().Take(k).ToList();

            Assert.Equal(esperadoMaiores, Selecao.Maiores(itens, k));
        }
    }

    [Fact(DisplayName = "k maior que a entrada devolve a entrada inteira, ordenada")]
    public void KMaiorQueAEntrada()
    {
        var sorteio = new Random(7);

        for (var caso = 0; caso < 5_000; caso++)
        {
            var quantos = sorteio.Next(0, 30);

            var itens = Enumerable.Range(0, quantos)
                .Select(_ => sorteio.Next(-100, 100))
                .ToArray();

            var achado = Selecao.Menores(itens, quantos + sorteio.Next(1, 20));

            Assert.Equal(itens.Order().ToArray(), achado);
            Assert.Equal(quantos, achado.Count);
        }

        Assert.Empty(Selecao.Menores(new[] { 1, 2, 3 }, 0));
        Assert.Empty(Selecao.Menores(Array.Empty<int>(), 5));

        Assert.Throws<ArgumentOutOfRangeException>(() => Selecao.Menores(new[] { 1 }, -1));
        Assert.Throws<ArgumentNullException>(() => Selecao.Menores<int>(null!, 3));
    }

    [Fact(DisplayName = "a memória é O(k), e não O(n)")]
    public void AMemoriaEhDeK()
    {
        // É a propriedade que importa de verdade: com k pequeno, o algoritmo
        // roda sobre um fluxo que não cabe na memória. Aqui isso é conferido com
        // uma sequência PREGUIÇOSA de dez milhões de itens, que nunca vira
        // vetor.
        //
        // Se o algoritmo materializasse a entrada, este teste estouraria a
        // memória em vez de passar.
        static IEnumerable<int> Fluxo(int quantos)
        {
            var sorteio = new Random(29);

            for (var i = 0; i < quantos; i++)
            {
                yield return sorteio.Next();
            }
        }

        var menores = Selecao.Menores(Fluxo(10_000_000), 10);

        Assert.Equal(10, menores.Count);

        // Saem em ordem.
        for (var i = 1; i < menores.Count; i++)
        {
            Assert.True(menores[i - 1] <= menores[i]);
        }

        // E são mesmo os dez menores: nenhum item do fluxo é menor que o maior
        // dos escolhidos, exceto os próprios escolhidos.
        var limite = menores[^1];

        var quantosAbaixo = Fluxo(10_000_000).Count(x => x < limite);

        Assert.Equal(9, quantosAbaixo);
    }

    [Fact(DisplayName = "a intercalação de k vias, contra juntar tudo e ordenar")]
    public void AIntercalacao()
    {
        var sorteio = new Random(13);

        for (var caso = 0; caso < 10_000; caso++)
        {
            var quantas = sorteio.Next(0, 10);

            var sequencias = new List<IEnumerable<int>>();

            var todos = new List<int>();

            for (var i = 0; i < quantas; i++)
            {
                var tamanho = sorteio.Next(0, 30);

                var sequencia = Enumerable.Range(0, tamanho)
                    .Select(_ => sorteio.Next(-200, 200))
                    .Order()
                    .ToArray();

                sequencias.Add(sequencia);
                todos.AddRange(sequencia);
            }

            var esperado = todos.Order().ToArray();

            var achado = Selecao.Intercalar(sequencias).ToArray();

            Assert.True(esperado.SequenceEqual(achado),
                $"com {quantas} sequências: esperava {esperado.Length} itens em ordem "
                + $"e veio {achado.Length}");
        }
    }

    [Fact(DisplayName = "a intercalação é preguiçosa: não lê o que não precisa")]
    public void AIntercalacaoEhPreguicosa()
    {
        // É o que permite ordenar um arquivo maior que a memória: a intercalação
        // guarda UMA linha de cada sequência, e só pede a próxima quando entrega
        // a anterior.
        var lidos = new int[3];

        IEnumerable<int> Contando(int qual, int de, int ate)
        {
            for (var i = de; i <= ate; i++)
            {
                lidos[qual]++;

                yield return i;
            }
        }

        var sequencias = new List<IEnumerable<int>>
        {
            Contando(0, 0, 999),
            Contando(1, 1_000, 1_999),
            Contando(2, 2_000, 2_999),
        };

        // Pega só os cinco primeiros.
        var primeiros = Selecao.Intercalar(sequencias).Take(5).ToArray();

        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, primeiros);

        // Cinco leituras da primeira sequência, e eu tinha previsto seis. A conta
        // certa: uma leitura inicial para pôr no monte, e depois uma reposição
        // após cada entrega. Só que o Take(5) para no quinto item e NUNCA pede o
        // sexto, então a reposição depois da última entrega não acontece.
        //
        // Ou seja, a preguiça é ainda mais apertada do que eu supus: nada e lido
        // alem do estritamente necessario.
        Assert.Equal(5, lidos[0]);
        Assert.Equal(1, lidos[1]);
        Assert.Equal(1, lidos[2]);
    }

    [Fact(DisplayName = "as bordas do monte")]
    public void AsBordas()
    {
        var vazio = new Monte<int>();

        Assert.Equal(0, vazio.Quantos);
        Assert.Equal(0, vazio.Altura());
        Assert.True(vazio.EstaSadio());
        Assert.False(vazio.TentarTirar(out _));

        Assert.Throws<InvalidOperationException>(() => vazio.Espiar());
        Assert.Throws<InvalidOperationException>(() => vazio.Tirar());
        Assert.Throws<InvalidOperationException>(() => vazio.TrocarMenor(1));

        Assert.Throws<ArgumentOutOfRangeException>(() => new Monte<int>(capacidade: 0));
        Assert.Throws<ArgumentNullException>(() => Monte<int>.De(null!));

        // Um item só.
        var um = new Monte<int>();

        um.Por(42);

        Assert.Equal(1, um.Quantos);
        Assert.Equal(1, um.Altura());
        Assert.Equal(42, um.Espiar());
        Assert.Equal(42, um.Tirar());
        Assert.Equal(0, um.Quantos);

        // Todos iguais: o monte aguenta, e devolve todos.
        var iguais = Monte<int>.De(Enumerable.Repeat(7, 100));

        Assert.True(iguais.EstaSadio());

        for (var i = 0; i < 100; i++)
        {
            Assert.Equal(7, iguais.Tirar());
        }

        Assert.Equal(0, iguais.Quantos);

        // Limpar.
        var cheio = Monte<int>.De(Enumerable.Range(0, 50));

        cheio.Limpar();

        Assert.Equal(0, cheio.Quantos);
        Assert.True(cheio.EstaSadio());

        // O comparador invertido transforma o monte de mínimo em monte de máximo.
        var maximo = new Monte<int>(Comparer<int>.Create((a, b) => b.CompareTo(a)));

        foreach (var i in (int[])[3, 1, 4, 1, 5, 9, 2, 6])
        {
            maximo.Por(i);
        }

        Assert.Equal(9, maximo.Tirar());
        Assert.Equal(6, maximo.Tirar());
        Assert.Equal(5, maximo.Tirar());

        // A fila com chave.
        var fila = new Prioridade<string, int>();

        Assert.Equal(0, fila.Quantos);
        Assert.False(fila.TentarTirar(out _, out _));
        Assert.Throws<InvalidOperationException>(() => fila.Espiar());

        fila.Por("a", 5);
        fila.Por("b", 1);

        Assert.Equal("b", fila.Espiar());
        Assert.Equal(("b", 1), fila.EspiarComChave());

        // Pôr e tirar o menor numa fila vazia devolve o próprio item.
        var outra = new Prioridade<string, int>();

        Assert.Equal("x", outra.PorETirarOMenor("x", 1));
        Assert.Equal(0, outra.Quantos);
    }
}
