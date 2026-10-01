namespace Conde.Fila;

/// <summary>
/// Um monte binário: a fila de prioridade que cabe num vetor.
/// </summary>
/// <remarks>
/// <para>
/// A ideia que faz tudo funcionar é guardar uma árvore num vetor sem ponteiro
/// nenhum: o filho esquerdo do índice <c>i</c> é <c>2i+1</c>, o direito é
/// <c>2i+2</c>, e o pai é <c>(i−1)/2</c>. Não há nó, não há alocação por
/// elemento, e a árvore é sempre completa porque o vetor não tem buracos.
/// </para>
/// <para>
/// A invariante é fraca de propósito: <b>todo pai é menor ou igual aos filhos</b>,
/// e nada mais. Um monte não é ordenado, e isso é o ponto. Manter a ordem total
/// custaria <c>O(n)</c> por inserção; manter só a relação pai-filho custa
/// <c>O(log n)</c>, e ainda assim o menor está sempre na posição 0.
/// </para>
/// <para>
/// É a estrutura por baixo do Dijkstra, do escalonador de todo sistema
/// operacional, e de qualquer coisa que diga "o próximo a ser atendido".
/// </para>
/// </remarks>
public sealed class Monte<T>
{
    private T[] _itens;
    private readonly IComparer<T> _comparar;

    /// <summary>Quantos itens há.</summary>
    public int Quantos { get; private set; }

    /// <summary>Monta um monte vazio.</summary>
    public Monte(IComparer<T>? comparador = null, int capacidade = 4)
    {
        if (capacidade < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(capacidade), capacidade, "pelo menos 1");
        }

        _itens = new T[capacidade];
        _comparar = comparador ?? Comparer<T>.Default;
    }

    /// <summary>
    /// Monta um monte a partir de uma coleção, em <c>O(n)</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Inserir os <c>n</c> itens um a um custaria <c>O(n·log n)</c>. Montar de
    /// baixo para cima custa <c>O(n)</c>, e a diferença não é óbvia: parece que
    /// descer <c>n/2</c> nós deveria custar <c>n/2 · log n</c>.
    /// </para>
    /// <para>
    /// A conta certa é outra. Metade dos nós são folhas e não descem nada; um
    /// quarto desce no máximo um nível; um oitavo desce dois. A soma
    /// <c>n/4·1 + n/8·2 + n/16·3 + …</c> converge para <c>n</c>, e não para
    /// <c>n·log n</c>.
    /// </para>
    /// </remarks>
    public static Monte<T> De(IEnumerable<T> itens, IComparer<T>? comparador = null)
    {
        ArgumentNullException.ThrowIfNull(itens);

        var vetor = itens.ToArray();

        var monte = new Monte<T>(comparador, Math.Max(1, vetor.Length))
        {
            _itens = vetor,
            Quantos = vetor.Length,
        };

        // De baixo para cima, a partir do último nó que tem filho.
        for (var i = vetor.Length / 2 - 1; i >= 0; i--)
        {
            monte.Descer(i);
        }

        return monte;
    }

    /// <summary>Põe um item.</summary>
    public void Por(T item)
    {
        if (Quantos == _itens.Length)
        {
            Array.Resize(ref _itens, _itens.Length * 2);
        }

        _itens[Quantos] = item;

        Subir(Quantos);

        Quantos++;
    }

    /// <summary>O menor item, sem tirar.</summary>
    public T Espiar() => Quantos == 0
        ? throw new InvalidOperationException("o monte está vazio")
        : _itens[0];

    /// <summary>Tira o menor item.</summary>
    public T Tirar()
    {
        if (Quantos == 0)
        {
            throw new InvalidOperationException("o monte está vazio");
        }

        var menor = _itens[0];

        Quantos--;

        if (Quantos > 0)
        {
            _itens[0] = _itens[Quantos];

            Descer(0);
        }

        // Limpa a posição que saiu, para não segurar uma referência viva. Num
        // monte de objetos grandes isso é a diferença entre liberar a memória e
        // não liberar.
        _itens[Quantos] = default!;

        return menor;
    }

    /// <summary>Tira o menor, devolvendo falso se estiver vazio.</summary>
    public bool TentarTirar(out T item)
    {
        if (Quantos == 0)
        {
            item = default!;

            return false;
        }

        item = Tirar();

        return true;
    }

    /// <summary>
    /// Troca o menor por outro item, numa operação só.
    /// </summary>
    /// <remarks>
    /// Tirar e pôr custa duas descidas; trocar custa uma. É a operação que um
    /// laço de "pegue o menor, processe, devolva outro" usa, e ela aparece no
    /// algoritmo de intercalação de k vias e na seleção dos k maiores.
    /// </remarks>
    public T TrocarMenor(T item)
    {
        if (Quantos == 0)
        {
            throw new InvalidOperationException("o monte está vazio");
        }

        var menor = _itens[0];

        _itens[0] = item;

        Descer(0);

        return menor;
    }

    /// <summary>Esvazia.</summary>
    public void Limpar()
    {
        Array.Clear(_itens, 0, Quantos);

        Quantos = 0;
    }

    /// <summary>Os itens, na ordem interna do vetor, que NÃO é a ordem de prioridade.</summary>
    /// <remarks>
    /// Expor isto parece estranho e serve a um teste: a invariante do monte é
    /// sobre a relação pai-filho, e conferi-la exige ver o vetor cru. Quem
    /// quiser os itens em ordem tem de tirá-los um a um.
    /// </remarks>
    public ReadOnlySpan<T> ComoVetor() => _itens.AsSpan(0, Quantos);

    private void Subir(int onde)
    {
        var item = _itens[onde];

        while (onde > 0)
        {
            var pai = (onde - 1) / 2;

            if (_comparar.Compare(item, _itens[pai]) >= 0)
            {
                break;
            }

            _itens[onde] = _itens[pai];

            onde = pai;
        }

        _itens[onde] = item;
    }

    private void Descer(int onde)
    {
        var item = _itens[onde];

        while (true)
        {
            var esquerda = 2 * onde + 1;

            if (esquerda >= Quantos)
            {
                break;
            }

            var menor = esquerda;

            var direita = esquerda + 1;

            if (direita < Quantos && _comparar.Compare(_itens[direita], _itens[esquerda]) < 0)
            {
                menor = direita;
            }

            if (_comparar.Compare(_itens[menor], item) >= 0)
            {
                break;
            }

            _itens[onde] = _itens[menor];

            onde = menor;
        }

        _itens[onde] = item;
    }

    /// <summary>Confere a invariante: todo pai é menor ou igual aos filhos.</summary>
    /// <remarks>
    /// Existe para os testes, e é o tipo de método que vale o custo de estar na
    /// classe. Um monte com a invariante quebrada <b>continua respondendo</b>:
    /// ele devolve itens, só que na ordem errada, e nenhum teste de exemplo
    /// pegaria isso.
    /// </remarks>
    public bool EstaSadio()
    {
        for (var pai = 0; pai < Quantos / 2; pai++)
        {
            var esquerda = 2 * pai + 1;
            var direita = esquerda + 1;

            if (esquerda < Quantos && _comparar.Compare(_itens[pai], _itens[esquerda]) > 0)
            {
                return false;
            }

            if (direita < Quantos && _comparar.Compare(_itens[pai], _itens[direita]) > 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>A altura do monte, que é o custo de uma operação.</summary>
    public int Altura() => Quantos == 0 ? 0 : (int)Math.Floor(Math.Log2(Quantos)) + 1;
}
