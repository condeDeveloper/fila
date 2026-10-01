using Xunit;

namespace Conde.Fila.Testes;

/// <summary>
/// O juiz é o <c>PriorityQueue</c> do .NET, mais a invariante depois de cada operação.
/// </summary>
/// <remarks>
/// <para>
/// O <c>PriorityQueue</c> chegou no .NET 6 e é, por baixo, um monte binário
/// quaternário. Comparar com ele é comparar com a mesma estrutura escrita por
/// quem mantém a plataforma.
/// </para>
/// <para>
/// Mas o juiz mais importante é o outro, e vale explicar por quê: um monte com a
/// invariante quebrada <b>continua respondendo</b>. Ele devolve itens, só que na
/// ordem errada, e devolve a quantidade certa. Comparar a sequência de saída com
/// a do .NET pegaria isso, e conferir a invariante <b>depois de cada operação</b>
/// diz exatamente onde quebrou.
/// </para>
/// </remarks>
public class JuizTest
{
    [Fact(DisplayName = "um milhão de operações sorteadas contra o PriorityQueue do .NET")]
    public void ContraOPriorityQueue()
    {
        var sorteio = new Random(29);

        var meu = new Monte<int>();
        var doNet = new PriorityQueue<int, int>();

        for (var passo = 0; passo < 1_000_000; passo++)
        {
            // Dois terços de inserção, um de remoção: a fila cresce, o que
            // exercita o redimensionamento e as descidas longas.
            if (sorteio.Next(3) < 2 || meu.Quantos == 0)
            {
                var valor = sorteio.Next(-100_000, 100_000);

                meu.Por(valor);
                doNet.Enqueue(valor, valor);
            }
            else
            {
                Assert.Equal(doNet.Peek(), meu.Espiar());
                Assert.Equal(doNet.Dequeue(), meu.Tirar());
            }

            Assert.Equal(doNet.Count, meu.Quantos);
        }

        // E o resto sai na mesma ordem.
        while (meu.Quantos > 0)
        {
            Assert.Equal(doNet.Dequeue(), meu.Tirar());
        }

        Assert.Equal(0, doNet.Count);
    }

    [Fact(DisplayName = "a invariante vale depois de CADA operação")]
    public void AInvarianteValeSempre()
    {
        // É o juiz que o PriorityQueue não é. Um monte torto responde certo em
        // muitos casos e erra em alguns, e a comparação de saída só pega o erro
        // quando ele acontece; a invariante pega a hora em que ele nasce.
        var sorteio = new Random(7);

        var monte = new Monte<int>();

        var esperados = new List<int>();

        for (var passo = 0; passo < 50_000; passo++)
        {
            if (sorteio.Next(3) < 2 || monte.Quantos == 0)
            {
                var valor = sorteio.Next(-1_000, 1_000);

                monte.Por(valor);
                esperados.Add(valor);

                Assert.True(monte.EstaSadio(), $"quebrou ao pôr {valor} no passo {passo}");
            }
            else
            {
                var menor = monte.Tirar();

                Assert.Equal(esperados.Min(), menor);

                esperados.Remove(menor);

                Assert.True(monte.EstaSadio(), $"quebrou ao tirar {menor} no passo {passo}");
            }

            Assert.Equal(esperados.Count, monte.Quantos);

            // A altura é logarítmica, sempre. Num monte torto ela cresceria.
            if (monte.Quantos > 0)
            {
                Assert.Equal((int)Math.Floor(Math.Log2(monte.Quantos)) + 1, monte.Altura());
            }
        }
    }

    [Fact(DisplayName = "montar de uma vez dá o mesmo monte que inserir um a um")]
    public void MontarDeUmaVez()
    {
        // A construção de baixo para cima é O(n) e a inserção um a um é
        // O(n log n). Os dois montes não precisam ter o MESMO vetor interno,
        // mas têm de devolver a mesma sequência e respeitar a invariante.
        var sorteio = new Random(13);

        for (var caso = 0; caso < 5_000; caso++)
        {
            var quantos = sorteio.Next(0, 200);

            var itens = Enumerable.Range(0, quantos)
                .Select(_ => sorteio.Next(-500, 500))
                .ToArray();

            var deUmaVez = Monte<int>.De(itens);

            Assert.True(deUmaVez.EstaSadio(), "o monte construído de uma vez saiu torto");
            Assert.Equal(quantos, deUmaVez.Quantos);

            var umAUm = new Monte<int>();

            foreach (var item in itens)
            {
                umAUm.Por(item);
            }

            // As duas sequências de saída são iguais.
            var esperado = itens.Order().ToArray();

            var deUmaVezSaida = new List<int>();
            var umAUmSaida = new List<int>();

            while (deUmaVez.TentarTirar(out var item))
            {
                deUmaVezSaida.Add(item);
            }

            while (umAUm.TentarTirar(out var item))
            {
                umAUmSaida.Add(item);
            }

            Assert.Equal(esperado, deUmaVezSaida);
            Assert.Equal(esperado, umAUmSaida);
        }
    }

    [Fact(DisplayName = "trocar o menor equivale a tirar e pôr, com uma descida só")]
    public void TrocarOMenor()
    {
        var sorteio = new Random(11);

        for (var caso = 0; caso < 20_000; caso++)
        {
            var quantos = sorteio.Next(1, 60);

            var itens = Enumerable.Range(0, quantos)
                .Select(_ => sorteio.Next(-200, 200))
                .ToArray();

            var novo = sorteio.Next(-200, 200);

            // Pelo caminho curto.
            var curto = Monte<int>.De(itens);

            var tirado = curto.TrocarMenor(novo);

            Assert.True(curto.EstaSadio());

            // Pelo caminho longo.
            var longo = Monte<int>.De(itens);

            var tiradoLongo = longo.Tirar();

            longo.Por(novo);

            Assert.Equal(tiradoLongo, tirado);
            Assert.Equal(longo.Quantos, curto.Quantos);

            var saidaCurto = new List<int>();
            var saidaLongo = new List<int>();

            while (curto.TentarTirar(out var item))
            {
                saidaCurto.Add(item);
            }

            while (longo.TentarTirar(out var item))
            {
                saidaLongo.Add(item);
            }

            Assert.Equal(saidaLongo, saidaCurto);
        }
    }

    [Fact(DisplayName = "a fila com chave separada, contra o PriorityQueue")]
    public void AFilaComChave()
    {
        var sorteio = new Random(29);

        var minha = new Prioridade<string, int>();
        var doNet = new PriorityQueue<string, int>();

        for (var passo = 0; passo < 200_000; passo++)
        {
            if (sorteio.Next(3) < 2 || minha.Quantos == 0)
            {
                var chave = sorteio.Next(0, 1_000);

                var item = $"tarefa-{passo}";

                minha.Por(item, chave);
                doNet.Enqueue(item, chave);
            }
            else
            {
                // Com chaves repetidas a ordem do desempate é arbitrária nos
                // dois, então o que se compara é a CHAVE e não o item.
                doNet.TryDequeue(out _, out var chaveDoNet);

                Assert.True(minha.TentarTirar(out _, out var minhaChave));

                Assert.Equal(chaveDoNet, minhaChave);
            }

            Assert.Equal(doNet.Count, minha.Quantos);
        }

        Assert.True(minha.EstaSadia());
    }

    [Fact(DisplayName = "a fila estável mantém a ordem de chegada, e a comum não garante")]
    public void AEstabilidade()
    {
        // O PriorityQueue do .NET documenta que a ordem de itens de mesma
        // prioridade é arbitrária. Aqui a estabilidade é opcional, e este teste
        // mede as duas: a estável NUNCA troca, e a comum troca em algum caso.
        var sorteio = new Random(7);

        var trocouNaComum = false;

        for (var caso = 0; caso < 2_000; caso++)
        {
            var quantos = sorteio.Next(2, 50);

            // Poucas prioridades e muitos itens: é assim que o empate aparece.
            var entradas = Enumerable.Range(0, quantos)
                .Select(i => (Item: i, Chave: sorteio.Next(0, 3)))
                .ToArray();

            foreach (var estavel in (bool[])[true, false])
            {
                var fila = new Prioridade<int, int>(estavel: estavel);

                foreach (var (item, chave) in entradas)
                {
                    fila.Por(item, chave);
                }

                var saida = new List<(int Item, int Chave)>();

                while (fila.TentarTirar(out var item, out var chave))
                {
                    saida.Add((item, chave));
                }

                // Ordenado por chave, nos dois casos.
                for (var i = 1; i < saida.Count; i++)
                {
                    Assert.True(saida[i - 1].Chave <= saida[i].Chave);
                }

                // Estável: dentro da mesma chave, a ordem de entrada.
                var manteve = true;

                for (var i = 1; i < saida.Count; i++)
                {
                    if (saida[i - 1].Chave == saida[i].Chave
                        && saida[i - 1].Item > saida[i].Item)
                    {
                        manteve = false;

                        break;
                    }
                }

                if (estavel)
                {
                    Assert.True(manteve, "a fila estável trocou a ordem de itens de chave igual");
                }
                else if (!manteve)
                {
                    trocouNaComum = true;
                }
            }
        }

        Assert.True(trocouNaComum,
            "a fila comum não trocou a ordem em nenhum dos 2.000 casos; ou o teste "
            + "não exercitou empates, ou ela virou estável por acidente");
    }
}
