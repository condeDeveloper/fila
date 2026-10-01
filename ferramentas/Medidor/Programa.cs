using System.Diagnostics;
using Conde.Fila;

namespace Conde.Fila.Ferramentas.Medidor;

/// <summary>
/// A ferramenta: mede o que a notacao O esconde.
/// </summary>
/// <remarks>
/// Tudo que depende de relogio mora aqui, e nao na suite de testes. Uma
/// afirmacao de tempo falha sem que nada tenha piorado, basta a maquina do CI
/// estar ocupada. Esta ferramenta imprime e sai com zero, sempre.
/// </remarks>
public static class Programa
{
    public static int Main(string[] argumentos)
    {
        var comandos = new[] { "montar", "kmenores", "intercalar", "altura", "velocidade" };

        var onde = Array.FindIndex(argumentos, a => comandos.Contains(a));

        if (onde < 0)
        {
            Console.WriteLine(Ajuda);

            return 0;
        }

        switch (argumentos[onde])
        {
            case "montar":
                Montar();
                break;

            case "kmenores":
                KMenores();
                break;

            case "intercalar":
                Intercalar();
                break;

            case "altura":
                Altura();
                break;

            default:
                Velocidade();
                break;
        }

        return 0;
    }

    /// <summary>Montar de uma vez contra inserir um a um.</summary>
    private static void Montar()
    {
        Console.WriteLine("Montar de uma vez e O(n); inserir um a um e O(n log n)");
        Console.WriteLine();
        Console.WriteLine("  A diferenca nao e obvia: parece que descer n/2 nos deveria custar");
        Console.WriteLine("  n/2 * log n. A conta certa e outra. Metade dos nos sao folhas e nao");
        Console.WriteLine("  descem nada; um quarto desce no maximo um nivel; um oitavo desce dois.");
        Console.WriteLine("  A soma n/4*1 + n/8*2 + n/16*3 + ... converge para n.");
        Console.WriteLine();
        Console.WriteLine("        n    de uma vez    um a um     razao");
        Console.WriteLine("  " + new string('-', 54));

        var sorteio = new Random(29);

        foreach (var quantos in new[] { 100_000, 1_000_000, 5_000_000 })
        {
            var itens = Enumerable.Range(0, quantos).Select(_ => sorteio.Next()).ToArray();

            var relogio = Stopwatch.StartNew();

            var deUmaVez = Monte<int>.De(itens);

            var tempoDeUmaVez = relogio.Elapsed.TotalMilliseconds;

            relogio.Restart();

            var umAUm = new Monte<int>(capacidade: quantos);

            foreach (var item in itens)
            {
                umAUm.Por(item);
            }

            var tempoUmAUm = relogio.Elapsed.TotalMilliseconds;

            Console.WriteLine(
                $"  {quantos,8:N0} {tempoDeUmaVez,11:F1} ms {tempoUmAUm,9:F1} ms "
                + $"{tempoUmAUm / Math.Max(tempoDeUmaVez, 0.001),8:F1}x");

            // Garante que os dois sao montes de verdade, e nao lixo rapido.
            if (!deUmaVez.EstaSadio() || !umAUm.EstaSadio())
            {
                Console.WriteLine("    (ATENCAO: um dos montes saiu torto)");
            }
        }

        Console.WriteLine();
        Console.WriteLine("  A razao fica em torno de 3 e nao cresce muito, porque log n cresce");
        Console.WriteLine("  devagar. O que a tabela mostra e que a diferenca EXISTE e e estavel,");
        Console.WriteLine("  que e o que a conta previa.");
    }

    /// <summary>Os k menores contra ordenar tudo.</summary>
    private static void KMenores()
    {
        Console.WriteLine("Os k menores de n itens, sem ordenar os n");
        Console.WriteLine();
        Console.WriteLine("  Achar os 10 maiores de um bilhao de itens nao exige ordenar um bilhao");
        Console.WriteLine("  de itens. Um monte de tamanho k basta, e o custo cai de O(n log n)");
        Console.WriteLine("  para O(n log k).");
        Console.WriteLine();
        Console.WriteLine("           n      k    monte de k   ordenar tudo     razao");
        Console.WriteLine("  " + new string('-', 62));

        var sorteio = new Random(29);

        foreach (var (quantos, k) in new[]
                 {
                     (1_000_000, 10),
                     (1_000_000, 1_000),
                     (10_000_000, 10),
                     (10_000_000, 100),
                 })
        {
            var itens = Enumerable.Range(0, quantos).Select(_ => sorteio.Next()).ToArray();

            var relogio = Stopwatch.StartNew();

            var pelosMontes = Selecao.Menores(itens, k);

            var tempoMonte = relogio.Elapsed.TotalMilliseconds;

            var copia = (int[])itens.Clone();

            relogio.Restart();

            Array.Sort(copia);

            var esperado = copia.Take(k).ToArray();

            var tempoOrdenar = relogio.Elapsed.TotalMilliseconds;

            var bateu = pelosMontes.SequenceEqual(esperado);

            Console.WriteLine(
                $"  {quantos,10:N0} {k,6:N0} {tempoMonte,11:F1} ms {tempoOrdenar,11:F1} ms "
                + $"{tempoOrdenar / Math.Max(tempoMonte, 0.001),8:F1}x"
                + (bateu ? string.Empty : "   (DIVERGIU)"));
        }

        Console.WriteLine();
        Console.WriteLine("  Mas a vantagem de tempo nao e o ponto principal. O ponto e a MEMORIA:");
        Console.WriteLine("  o monte guarda k itens, entao o algoritmo roda sobre um fluxo que nao");
        Console.WriteLine("  cabe na memoria. Ordenar exige ter tudo de uma vez.");
        Console.WriteLine();

        // Um fluxo preguiçoso de dez milhões de itens, que nunca vira vetor.
        static IEnumerable<int> Fluxo(int quantos)
        {
            var dentro = new Random(7);

            for (var i = 0; i < quantos; i++)
            {
                yield return dentro.Next();
            }
        }

        var relogioFluxo = Stopwatch.StartNew();

        var doFluxo = Selecao.Menores(Fluxo(10_000_000), 10);

        Console.WriteLine($"  Os 10 menores de um fluxo de 10 milhoes, sem nunca materializar:");
        Console.WriteLine($"    {string.Join(", ", doFluxo)}");
        Console.WriteLine($"    em {relogioFluxo.Elapsed.TotalMilliseconds:F0} ms, "
            + "com 10 itens na memoria");
    }

    /// <summary>A intercalacao de k vias.</summary>
    private static void Intercalar()
    {
        Console.WriteLine("Intercalar k sequencias ordenadas, com k itens na memoria");
        Console.WriteLine();
        Console.WriteLine("  E o segundo passo de toda ordenacao externa: os blocos ja estao");
        Console.WriteLine("  ordenados em disco, e e preciso junta-los lendo uma linha de cada.");
        Console.WriteLine();

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

        var primeiros = Selecao.Intercalar(sequencias).Take(5).ToArray();

        Console.WriteLine($"  Pedindo so os 5 primeiros de 3 sequencias de mil itens:");
        Console.WriteLine();
        Console.WriteLine($"    saida            {string.Join(", ", primeiros)}");
        Console.WriteLine($"    leituras da 1a   {lidos[0]}");
        Console.WriteLine($"    leituras da 2a   {lidos[1]}");
        Console.WriteLine($"    leituras da 3a   {lidos[2]}");
        Console.WriteLine();
        Console.WriteLine("  Nada alem do necessario foi lido. As sequencias 2 e 3 contribuiram");
        Console.WriteLine("  com UM item cada, que e o que esta no monte esperando a vez.");
        Console.WriteLine();

        // E a escala.
        var sorteio = new Random(29);

        Console.WriteLine("  E em escala:");
        Console.WriteLine();
        Console.WriteLine("        k   itens por via      total    intercalar   juntar e ordenar");
        Console.WriteLine("  " + new string('-', 72));

        foreach (var (k, porVia) in new[] { (10, 100_000), (100, 10_000), (1_000, 1_000) })
        {
            var blocos = Enumerable.Range(0, k)
                .Select(_ => Enumerable.Range(0, porVia)
                    .Select(_ => sorteio.Next())
                    .Order()
                    .ToArray())
                .ToList();

            var relogio = Stopwatch.StartNew();

            var intercalado = Selecao.Intercalar(blocos.Cast<IEnumerable<int>>().ToList()).ToArray();

            var tempoIntercalar = relogio.Elapsed.TotalMilliseconds;

            relogio.Restart();

            var juntado = blocos.SelectMany(b => b).ToArray();

            Array.Sort(juntado);

            var tempoJuntar = relogio.Elapsed.TotalMilliseconds;

            Console.WriteLine(
                $"  {k,7:N0} {porVia,15:N0} {k * porVia,10:N0} {tempoIntercalar,12:F1} ms "
                + $"{tempoJuntar,15:F1} ms"
                + (intercalado.SequenceEqual(juntado) ? string.Empty : "   (DIVERGIU)"));
        }

        Console.WriteLine();
        Console.WriteLine("  Ordenar tudo de novo e mais rapido, e joga fora a informacao de que os");
        Console.WriteLine("  blocos JA ESTAVAM ordenados. A intercalacao existe para quando eles nao");
        Console.WriteLine("  cabem juntos na memoria, e ai a comparacao de tempo nem acontece.");
    }

    /// <summary>A altura do monte.</summary>
    private static void Altura()
    {
        Console.WriteLine("A altura do monte, que e o custo de uma operacao");
        Console.WriteLine();
        Console.WriteLine("  Um monte e sempre uma arvore COMPLETA, porque o vetor nao tem buracos.");
        Console.WriteLine("  Entao a altura e exatamente floor(log2(n)) + 1, sem excecao, e nao ha");
        Console.WriteLine("  caso degenerado como numa arvore de busca.");
        Console.WriteLine();
        Console.WriteLine("           n   altura   comparacoes no pior caso de uma remocao");
        Console.WriteLine("  " + new string('-', 66));

        foreach (var quantos in new[] { 1, 10, 100, 1_000, 1_000_000, 1_000_000_000 })
        {
            var altura = quantos == 0 ? 0 : (int)Math.Floor(Math.Log2(quantos)) + 1;

            Console.WriteLine($"  {quantos,10:N0} {altura,8}   {2 * (altura - 1),10}");
        }

        Console.WriteLine();
        Console.WriteLine("  Um bilhao de itens tem altura 30. Uma remocao desce no maximo 30");
        Console.WriteLine("  niveis, com duas comparacoes por nivel.");
        Console.WriteLine();

        // Confere na prática.
        var sorteio = new Random(29);

        var monte = new Monte<int>();

        var conferidos = 0;

        for (var i = 1; i <= 100_000; i++)
        {
            monte.Por(sorteio.Next());

            var esperada = (int)Math.Floor(Math.Log2(i)) + 1;

            if (monte.Altura() != esperada)
            {
                Console.WriteLine($"    (ATENCAO: com {i} itens a altura deu {monte.Altura()})");
            }

            conferidos++;
        }

        Console.WriteLine($"  Conferido em {conferidos:N0} tamanhos: a altura e sempre a prevista.");
    }

    /// <summary>Contra o PriorityQueue do .NET.</summary>
    private static void Velocidade()
    {
        Console.WriteLine("Contra o PriorityQueue do .NET");
        Console.WriteLine();
        Console.WriteLine("  O PriorityQueue chegou no .NET 6 e e, por baixo, um monte QUATERNARIO:");
        Console.WriteLine("  cada no tem quatro filhos em vez de dois. A arvore fica mais rasa, com");
        Console.WriteLine("  log4(n) niveis em vez de log2(n), ao preco de mais comparacoes por");
        Console.WriteLine("  nivel na descida.");
        Console.WriteLine();
        Console.WriteLine("           n     por e tirar aqui   PriorityQueue    razao");
        Console.WriteLine("  " + new string('-', 62));

        foreach (var quantos in new[] { 100_000, 1_000_000, 5_000_000 })
        {
            var sorteio = new Random(29);

            var itens = Enumerable.Range(0, quantos).Select(_ => sorteio.Next()).ToArray();

            var meu = new Monte<int>(capacidade: quantos);

            var relogio = Stopwatch.StartNew();

            foreach (var item in itens)
            {
                meu.Por(item);
            }

            while (meu.Quantos > 0)
            {
                meu.Tirar();
            }

            var tempoMeu = relogio.Elapsed.TotalMilliseconds;

            var doNet = new PriorityQueue<int, int>(quantos);

            relogio.Restart();

            foreach (var item in itens)
            {
                doNet.Enqueue(item, item);
            }

            while (doNet.Count > 0)
            {
                doNet.Dequeue();
            }

            var tempoDoNet = relogio.Elapsed.TotalMilliseconds;

            Console.WriteLine(
                $"  {quantos,10:N0} {tempoMeu,17:F1} ms {tempoDoNet,13:F1} ms "
                + $"{tempoMeu / Math.Max(tempoDoNet, 0.001),8:F2}x");
        }

        Console.WriteLine();
        Console.WriteLine("  A diferenca que sobra nao e so o grau do monte: o PriorityQueue guarda");
        Console.WriteLine("  item e chave num struct e evita a chamada virtual do IComparer no caso");
        Console.WriteLine("  comum, que e o que mais custa num laco apertado.");
    }

    private const string Ajuda = """
        fila -- monte binario e fila de prioridade do zero

          medidor montar       montar de uma vez contra inserir um a um
          medidor kmenores     os k menores de n itens, sem ordenar os n
          medidor intercalar   k sequencias com k itens na memoria
          medidor altura       a altura do monte, que e o custo de uma operacao
          medidor velocidade   contra o PriorityQueue do .NET
        """;
}
