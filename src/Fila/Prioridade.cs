namespace Conde.Fila;

/// <summary>
/// Uma fila de prioridade com item e chave separados, como a do .NET.
/// </summary>
/// <remarks>
/// <para>
/// Separar o item da chave resolve o caso comum: ordenar por um campo que não é
/// o item. Sem isso, quem quer uma fila de tarefas por prazo precisa embrulhar
/// cada tarefa num par e escrever um comparador.
/// </para>
/// </remarks>
public sealed class Prioridade<TItem, TChave>
{
    private readonly Monte<(TChave Chave, long Ordem, TItem Item)> _monte;

    private long _proximaOrdem;

    private readonly bool _estavel;

    /// <summary>Quantos itens há.</summary>
    public int Quantos => _monte.Quantos;

    /// <summary>
    /// Monta a fila.
    /// </summary>
    /// <param name="comparador">Como comparar as chaves.</param>
    /// <param name="estavel">
    /// Se verdadeiro, itens de chave igual saem na ordem em que entraram.
    /// </param>
    /// <remarks>
    /// <para>
    /// A estabilidade <b>não vem de graça</b> num monte, e por isso é opcional
    /// aqui. Um monte troca itens distantes entre si, então dois itens de mesma
    /// prioridade saem em ordem arbitrária, e o <c>PriorityQueue</c> do .NET
    /// documenta isso explicitamente.
    /// </para>
    /// <para>
    /// A saída é um contador de entrada que serve de desempate. Custa oito bytes
    /// por item e uma comparação a mais, e é o preço de uma fila de atendimento
    /// em que quem chegou antes é atendido antes dentro da mesma prioridade.
    /// </para>
    /// </remarks>
    public Prioridade(IComparer<TChave>? comparador = null, bool estavel = false)
    {
        _estavel = estavel;

        var comparar = comparador ?? Comparer<TChave>.Default;

        _monte = new Monte<(TChave Chave, long Ordem, TItem Item)>(
            Comparer<(TChave Chave, long Ordem, TItem Item)>.Create((um, outro) =>
            {
                var pelaChave = comparar.Compare(um.Chave, outro.Chave);

                if (pelaChave != 0 || !estavel)
                {
                    return pelaChave;
                }

                return um.Ordem.CompareTo(outro.Ordem);
            }));
    }

    /// <summary>Põe um item com a chave dada.</summary>
    public void Por(TItem item, TChave chave) =>
        _monte.Por((chave, _estavel ? _proximaOrdem++ : 0, item));

    /// <summary>O item de menor chave, sem tirar.</summary>
    public TItem Espiar() => _monte.Espiar().Item;

    /// <summary>O item de menor chave e a chave dele, sem tirar.</summary>
    public (TItem Item, TChave Chave) EspiarComChave()
    {
        var (chave, _, item) = _monte.Espiar();

        return (item, chave);
    }

    /// <summary>Tira o item de menor chave.</summary>
    public TItem Tirar() => _monte.Tirar().Item;

    /// <summary>Tira o item de menor chave, devolvendo falso se estiver vazia.</summary>
    public bool TentarTirar(out TItem item, out TChave chave)
    {
        if (_monte.TentarTirar(out var tudo))
        {
            item = tudo.Item;
            chave = tudo.Chave;

            return true;
        }

        item = default!;
        chave = default!;

        return false;
    }

    /// <summary>Põe um item e tira o menor, numa operação só.</summary>
    public TItem PorETirarOMenor(TItem item, TChave chave)
    {
        if (Quantos == 0)
        {
            return item;
        }

        return _monte.TrocarMenor((chave, _estavel ? _proximaOrdem++ : 0, item)).Item;
    }

    /// <summary>Esvazia.</summary>
    public void Limpar() => _monte.Limpar();

    /// <summary>Confere a invariante do monte por baixo.</summary>
    public bool EstaSadia() => _monte.EstaSadio();
}

/// <summary>
/// Os k menores de uma sequência, sem ordenar tudo.
/// </summary>
/// <remarks>
/// <para>
/// Achar os 10 maiores de um bilhão de itens não exige ordenar um bilhão de
/// itens. Basta um monte de tamanho 10: para cada item, compare com o pior dos
/// que estão guardados, e troque se for melhor.
/// </para>
/// <para>
/// O custo cai de <c>O(n·log n)</c> para <c>O(n·log k)</c>, e a memória de
/// <c>O(n)</c> para <c>O(k)</c>. A segunda parte é a que importa de verdade: com
/// <c>k</c> pequeno, o algoritmo roda sobre um fluxo que não cabe na memória.
/// </para>
/// </remarks>
public static class Selecao
{
    /// <summary>
    /// Os <paramref name="k"/> menores, usando um monte de tamanho k.
    /// </summary>
    /// <remarks>
    /// O truque é o monte guardar o <b>maior</b> dos k menores no topo, com o
    /// comparador invertido. Assim a comparação "este item entra?" é uma olhada
    /// na raiz, que é <c>O(1)</c>.
    /// </remarks>
    public static List<T> Menores<T>(IEnumerable<T> itens, int k, IComparer<T>? comparador = null)
    {
        ArgumentNullException.ThrowIfNull(itens);

        if (k < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(k), k, "não é negativo");
        }

        if (k == 0)
        {
            return [];
        }

        var comparar = comparador ?? Comparer<T>.Default;

        // Invertido: o topo é o MAIOR dos guardados.
        var invertido = Comparer<T>.Create((um, outro) => comparar.Compare(outro, um));

        var monte = new Monte<T>(invertido, k);

        foreach (var item in itens)
        {
            if (monte.Quantos < k)
            {
                monte.Por(item);
            }
            else if (comparar.Compare(item, monte.Espiar()) < 0)
            {
                monte.TrocarMenor(item);
            }
        }

        var saida = new List<T>(monte.Quantos);

        while (monte.TentarTirar(out var item))
        {
            saida.Add(item);
        }

        // Saíram do maior para o menor, por causa do comparador invertido.
        saida.Reverse();

        return saida;
    }

    /// <summary>Os <paramref name="k"/> maiores.</summary>
    public static List<T> Maiores<T>(IEnumerable<T> itens, int k, IComparer<T>? comparador = null)
    {
        var comparar = comparador ?? Comparer<T>.Default;

        var invertido = Comparer<T>.Create((um, outro) => comparar.Compare(outro, um));

        return Menores(itens, k, invertido);
    }

    /// <summary>
    /// Intercala k sequências já ordenadas, mantendo a ordem.
    /// </summary>
    /// <remarks>
    /// <para>
    /// É o segundo passo de toda ordenação externa: os blocos já estão ordenados
    /// em disco, e é preciso juntá-los lendo uma linha de cada por vez.
    /// </para>
    /// <para>
    /// O monte guarda <b>uma linha de cada sequência</b>, então a memória é
    /// <c>O(k)</c> e não <c>O(n)</c>. É o que permite ordenar um arquivo maior
    /// que a memória.
    /// </para>
    /// </remarks>
    public static IEnumerable<T> Intercalar<T>(
        IReadOnlyList<IEnumerable<T>> sequencias, IComparer<T>? comparador = null)
    {
        ArgumentNullException.ThrowIfNull(sequencias);

        var comparar = comparador ?? Comparer<T>.Default;

        var monte = new Monte<(T Valor, int Qual)>(
            Comparer<(T Valor, int Qual)>.Create(
                (um, outro) => comparar.Compare(um.Valor, outro.Valor)));

        var leitores = sequencias.Select(s => s.GetEnumerator()).ToArray();

        try
        {
            for (var i = 0; i < leitores.Length; i++)
            {
                if (leitores[i].MoveNext())
                {
                    monte.Por((leitores[i].Current, i));
                }
            }

            while (monte.Quantos > 0)
            {
                var (valor, qual) = monte.Tirar();

                yield return valor;

                if (leitores[qual].MoveNext())
                {
                    monte.Por((leitores[qual].Current, qual));
                }
            }
        }
        finally
        {
            foreach (var leitor in leitores)
            {
                leitor.Dispose();
            }
        }
    }
}
