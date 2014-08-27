/* Copyright 2013-2014 Daikon Forge */
using UnityEngine;

using System;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Defines a simplified Generic List customized specifically for 
/// game development - Implements object pooling, minimizes memory
/// allocations during common operations, replaces common extension
/// methods with bespoke implementations that do not allocate 
/// iterators, etc.
/// </summary>
public class dfList<T> : IList<T>, IDisposable, IPoolable
{

	#region Object pooling 

	#region Static variables 

	// NOTE: Switched to Queue<object> in an attempt to work around 
	// a bug in the version of Mono used by Unity on iOS - http://stackoverflow.com/q/16542915
	private static Queue<object> pool = new Queue<object>( 1024 );

	#endregion 

	/// <summary>
	/// Releases all instances in the object pool.
	/// </summary>
	public static void ClearPool()
	{

		lock( pool )
		{
			pool.Clear();
			pool.TrimExcess();
		}

	}

	/// <summary>
	/// Returns a reference to a <see cref="dfList<T>"/> instance. If there are 
	/// available instances in the object pool, the first available instance will
	/// be returned. If there are no instances available, then a new instance
	/// will be created. Use the <see cref="Release"/> function to return the instance
	/// to the object pool.
	/// </summary>
	public static dfList<T> Obtain()
	{

		lock( pool )
		{

			if( pool.Count == 0 )
				return new dfList<T>();

			return (dfList<T>)pool.Dequeue();

		}

	}

	/// <summary>
	/// Returns a reference to a <see cref="dfList<T>"/> instance. If there are 
	/// available instances in the object pool, the first available instance will
	/// be returned. If there are no instances available, then a new instance
	/// will be created. Use the <see cref="Release"/> function to return the instance
	/// to the object pool.
	/// </summary>
	public static dfList<T> Obtain( int capacity )
	{

		var list = Obtain();

		list.EnsureCapacity( capacity );

		return list;

	}

	/// <summary>
	/// If the element type implements the IPoolable interface, will call the Release()
	/// method of each item in the collection, and the collection will be cleared.
	/// </summary>
	public void ReleaseItems()
	{

		if( !isElementTypePoolable )
			throw new InvalidOperationException( string.Format( "Element type {0} does not implement the {1} interface", typeof( T ).Name, typeof( IPoolable ).Name ) );

		for( int i = 0; i < count; i++ )
		{
			var item = items[ i ] as IPoolable;
			item.Release();
		}

		Clear();

	}

	/// <summary>
	/// Releases the <see cref="dfList"/> back to the object pool
	/// </summary>
	public void Release()
	{

		lock( pool )
		{

			if( autoReleaseItems && isElementTypePoolable )
			{
				autoReleaseItems = false;
				ReleaseItems();
			}
			else
			{
				Clear();
			}

			pool.Enqueue( this );
		}

	}

	#endregion

	#region Private instance fields

	private const int DEFAULT_CAPACITY = 128;

	private T[] items = new T[ DEFAULT_CAPACITY ];
	private int count = 0;

	private bool isElementTypeValueType = false;
	private bool isElementTypePoolable = false;
	private bool autoReleaseItems = false;

	#endregion

	#region Constructor 

	internal dfList()
	{

#if !UNITY_EDITOR && UNITY_METRO 
		isElementTypeValueType = typeof( T ).GetTypeInfo().IsValueType;
		isElementTypePoolable = typeof( IPoolable ).GetTypeInfo().IsAssignableFrom( typeof( T ).GetTypeInfo() );
#else
		isElementTypeValueType = typeof( T ).IsValueType;
		isElementTypePoolable = typeof( IPoolable ).IsAssignableFrom( typeof( T ) );
#endif

	}

	internal dfList( IList<T> listToClone )
		: this()
	{
		AddRange( listToClone );
	}

	internal dfList( int capacity )
		: this()
	{
		EnsureCapacity( capacity );
	}

	#endregion

	#region Public properties 

	/// <summary>
	/// If set to TRUE (defaults to FALSE), will attempt to call IPoolable.Release() on each contained item
	/// when the Release() method is called.
	/// </summary>
	public bool AutoReleaseItems
	{
		get { return this.autoReleaseItems; }
		set { autoReleaseItems = value; }
	}

	/// <summary>
	/// Returns the number of items in the list
	/// </summary>
	public int Count
	{
		get { return this.count; }
	}

	/// <summary>
	/// Returns the number of items this list can hold without needing to 
	/// resize the internal array (for internal use only)
	/// </summary>
	internal int Capacity
	{
		get { return this.items.Length; }
	}

	/// <summary>
	/// Gets a value indicating whether the list is read-only. Inherited from IList&lt;&gt;
	/// </summary>
	public bool IsReadOnly
	{
		get { return false; }
	}

	/// <summary>
	/// Gets/Sets the item at the specified index
	/// </summary>
	public T this[ int index ]
	{
		get
		{
			if( index < 0 || index > this.count - 1 )
				throw new IndexOutOfRangeException();
			return this.items[ index ];
		}
		set
		{
			if( index < 0 || index > this.count - 1 )
				throw new IndexOutOfRangeException();
			this.items[ index ] = value;
		}
	}

	/// <summary>
	/// Allows direct access to the underlying <see cref="System.Array"/>
	/// containing this list's data. This array will most likely contain more 
	/// elements than the list reports via the <see cref="Count"/> property.
	/// This property is intended for internal use by the UI library and should
	/// not be accessed by other code.
	/// </summary>
	internal T[] Items
	{ 
		get { return this.items; }
	}

	#endregion

	#region Public methods 

	/// <summary>
	/// Adds a new item to the end of the list. Provided only for call-level 
	/// compatability with code that treats this collection as a queue.
	/// </summary>
	public void Enqueue( T item )
	{
		lock( items )
		{
			this.Add( item );
		}
	}

	/// <summary>
	/// Returns the first item in the collection and removes it from the list. 
	/// Provided only for call-level compatability with code that treats this 
	/// collection as a queue.
	/// </summary>
	public T Dequeue()
	{

		lock( items )
		{

			if( this.count == 0 )
				throw new IndexOutOfRangeException();

			var item = this.items[ 0 ];

			this.RemoveAt( 0 );

			return item;

		}

	}

	/// <summary>
	/// Returns the last item in the collection and removes it frm the list.
	/// Provided only for call-level compatibility with code that treats this
	/// collection as a Stack.
	/// </summary>
	/// <returns></returns>
	public T Pop()
	{
		lock( items )
		{

			if( this.count == 0 )
				throw new IndexOutOfRangeException();

			var item = this.items[ this.count - 1 ];

			this.items[ this.count - 1 ] = default( T );

			this.count -= 1;

			return item;

		}
	}

	/// <summary>
	/// Returns a shallow copy of this <see cref="dfList<T>"/> instance 
	/// </summary>
	/// <returns></returns>
	public dfList<T> Clone()
	{

		var clone = Obtain( this.count );

		Array.Copy( this.items, 0, clone.items, 0, this.count );
			
		clone.count = this.count;
			
		return clone;

	}

	/// <summary>
	/// Reverses the order of the elements in the list
	/// </summary>
	public void Reverse()
	{
		Array.Reverse( this.items, 0, this.count );
	}

	/// <summary>
	/// Sorts the elements in the entire <see cref="dfList" /> using the default comparer.
	/// </summary>
	public void Sort()
	{
		Array.Sort( this.items, 0, this.count, null );
	}

	/// <summary>
	/// Sorts the elements in the entire <see cref="dfList" /> using the specified comparer.
	/// </summary>
	public void Sort( IComparer<T> comparer )
	{
		Array.Sort( this.items, 0, this.count, comparer );
	}

	/// <summary>
	/// Sorts the elements in the entire <see cref="System.Collections.Generic.List" /> using the specified <see cref="System.Comparison" />.
	/// </summary>
	/// <param name="comparison">The <see cref="System.Comparison" /> to use when comparing elements.</param>
	public void Sort( Comparison<T> comparison )
	{
		if( comparison == null )
		{
			throw new ArgumentNullException( "comparison" );
		}
		if( this.count > 0 )
		{
			using( var comparer = FunctorComparer.Obtain( comparison ) )
			{
				Array.Sort<T>( this.items, 0, this.count, comparer );
			}
		}
	}
		
	/// <summary>
	/// Ensures that the <see cref="dfList" /> has enough capacity to store <paramref name="Size"/> elements
	/// </summary>
	/// <param name="Size"></param>
	public void EnsureCapacity( int Size )
	{
		if( items.Length < Size )
		{
			var newSize = ( Size / DEFAULT_CAPACITY ) * DEFAULT_CAPACITY + DEFAULT_CAPACITY;
			Array.Resize<T>( ref this.items, newSize );
		}
	}

	/// <summary>
	/// Adds the elements of the specified collection to the end of the <see cref="dfList"/>
	/// </summary>
	public void AddRange( dfList<T> list )
	{

		var listCount = list.count;

		EnsureCapacity( this.count + listCount );
		Array.Copy( list.items, 0, this.items, this.count, listCount );
		this.count += listCount;

	}

	/// <summary>
	/// Adds the elements of the specified collection to the end of the <see cref="dfList"/>
	/// </summary>
	public void AddRange( IList<T> list )
	{

		var listCount = list.Count;

		EnsureCapacity( this.count + listCount );

		for( int i = 0; i < listCount; i++ )
		{
			this.items[ count++ ] = list[ i ]; 
		}

	}

	/// <summary>
	/// Adds the elements of the specified collection to the end of the <see cref="dfList"/>
	/// </summary>
	public void AddRange( T[] list )
	{

		var listLength = list.Length;

		EnsureCapacity( this.count + listLength );
		Array.Copy( list, 0, this.items, this.count, listLength );
		this.count += listLength;

	}

	/// <summary>
	/// Determines the index of a specific item in the collection
	/// </summary>
	public int IndexOf( T item )
	{
		return Array.IndexOf<T>( this.items, item, 0, this.count );
	}

	/// <summary>
	/// Inserts an item to the collection at the specified index
	/// </summary>
	/// <param name="index"></param>
	/// <param name="item"></param>
	public void Insert( int index, T item )
	{

		EnsureCapacity( this.count + 1 );

		if( index < this.count )
		{
			Array.Copy( this.items, index, this.items, index + 1, this.count - index );
		}

		this.items[ index ] = item;
		this.count += 1;

	}

	/// <summary>
	/// Inserts an array of items at the specified index
	/// </summary>
	public void InsertRange( int index, T[] array )
	{

		if( array == null )
			throw new ArgumentNullException( "items" );

		if( index < 0 || index > this.count )
			throw new ArgumentOutOfRangeException( "index" );

		EnsureCapacity( this.count + array.Length );

		if( index < this.count )
		{
			Array.Copy( this.items, index, this.items, index + array.Length, this.count - index );
		}

		array.CopyTo( this.items, index );

		this.count += array.Length;

	}

	/// <summary>
	/// Inserts a collection of items at the specified index
	/// </summary>
	public void InsertRange( int index, dfList<T> list )
	{

		if( list == null )
			throw new ArgumentNullException( "items" );

		if( index < 0 || index > this.count )
			throw new ArgumentOutOfRangeException( "index" );

		EnsureCapacity( this.count + list.count );

		if( index < this.count )
		{
			Array.Copy( this.items, index, this.items, index + list.count, this.count - index );
		}

		Array.Copy( list.items, 0, this.items, index, list.count );

		this.count += list.count;

	}

	/// <summary>
	/// Removes all items matching the predicate condition from the list
	/// </summary>
	public void RemoveAll( Predicate<T> predicate )
	{

		var index = 0;
		while( index < this.count )
		{
			if( predicate( items[ index ] ) )
			{
				RemoveAt( index );
			}
			else
			{
				index += 1;
			}
		}

	}

	/// <summary>
	/// Removes the item at the specified index
	/// </summary>
	public void RemoveAt( int index )
	{

		if( index >= this.count )
		{
			throw new ArgumentOutOfRangeException();
		}

		this.count -= 1;

		if( index < this.count )
		{
			Array.Copy( this.items, index + 1, this.items, index, this.count - index );
		}

		this.items[ this.count ] = default( T );

	}

	/// <summary>
	/// Removes <paramref name="length"/> items from the collection at the specified index
	/// </summary>
	public void RemoveRange( int index, int length )
	{

		if( index < 0 || length < 0 || this.count - index < length )
		{
			throw new ArgumentOutOfRangeException();
		}

		if( count > 0 )
		{

			this.count -= length;
			if( index < this.count )
			{
				Array.Copy( this.items, index + length, this.items, index, this.count - index );
			}

			Array.Clear( this.items, this.count, length );

		}

	}

	/// <summary>
	/// Adds an item to the collection
	/// </summary>
	public void Add( T item )
	{
		EnsureCapacity( this.count + 1 );
		this.items[ this.count++ ] = item;
	}

	/// <summary>
	/// Adds two items to the collection
	/// </summary>
	public void Add( T item0, T item1 )
	{
		EnsureCapacity( this.count + 2 );
		this.items[ this.count++ ] = item0;
		this.items[ this.count++ ] = item1;
	}

	/// <summary>
	/// Adds three items to the collection
	/// </summary>
	public void Add( T item0, T item1, T item2 )
	{
		EnsureCapacity( this.count + 3 );
		this.items[ this.count++ ] = item0;
		this.items[ this.count++ ] = item1;
		this.items[ this.count++ ] = item2;
	}

	/// <summary>
	/// Removes all items from the collection
	/// </summary>
	public void Clear()
	{

		if( !isElementTypeValueType )
		{
			Array.Clear( this.items, 0, this.items.Length );
		}

		this.count = 0;

	}

	/// <summary>
	/// Resizes the internal buffer to exactly match the number of elements in the collection
	/// </summary>
	public void TrimExcess()
	{
		Array.Resize( ref this.items, this.count );
	}

	/// <summary>
	/// Determines whether the collection contains the specified value
	/// </summary>
	public bool Contains( T item )
	{

		if( item == null )
		{
			for( int i = 0; i < this.count; i++ )
			{
				if( this.items[ i ] == null )
				{
					return true;
				}
			}
			return false;
		}

		EqualityComparer<T> comparer = EqualityComparer<T>.Default;

		for( int j = 0; j < this.count; j++ )
		{
			if( comparer.Equals( this.items[ j ], item ) )
			{
				return true;
			}
		}

		return false;

	}

	/// <summary>
	/// Copies the elements of the collection to a <see cref="System.Array"/> instance
	/// </summary>
	/// <param name="array"></param>
	public void CopyTo( T[] array )
	{
		CopyTo( array, 0 );
	}

	/// <summary>
	/// Copies the elements of the collection to an <see cref="System.Array"/> starting at the specified index
	/// </summary>
	public void CopyTo( T[] array, int arrayIndex )
	{
		Array.Copy( this.items, 0, array, arrayIndex, this.count );
	}

	/// <summary>
	/// Copies the elements of the collection to an <see cref="System.Array"/>
	/// </summary>
	/// <param name="sourceIndex">The starting position in the collection</param>
	/// <param name="dest">The destination array</param>
	/// <param name="destIndex">The position in the array to start copying to</param>
	/// <param name="length">How many elements to copy</param>
	public void CopyTo( int sourceIndex, T[] dest, int destIndex, int length )
	{
			
		if( sourceIndex + length > this.count )
			throw new IndexOutOfRangeException( "sourceIndex" );

		if( dest == null )
			throw new ArgumentNullException( "dest" );

		if( destIndex + length > dest.Length )
			throw new IndexOutOfRangeException( "destIndex" );

		Array.Copy( this.items, sourceIndex, dest, destIndex, length );

	}

	/// <summary>
	/// Removes the first occurrence of a specific object from the collection
	/// </summary>
	public bool Remove( T item )
	{

		var index = IndexOf( item );
		if( index == -1 )
			return false;

		RemoveAt( index );

		return true;

	}

	/// <summary>
	/// Returns a List&lt;T&gt; collection containing all elements of this collection
	/// </summary>
	/// <returns></returns>
	public List<T> ToList()
	{
		var list = new List<T>( this.count );
		list.AddRange( this.ToArray() );
		return list;
	}

	/// <summary>
	/// Returns an array containing all elements of this collection
	/// </summary>
	public T[] ToArray()
	{

		var array = new T[ this.count ];

		Array.Copy( this.items, 0, array, 0, this.count );

		return array;

	}

	/// <summary>
	/// Returns a subset of the collection's items as an array
	/// </summary>
	public T[] ToArray( int index, int length )
	{

		var array = new T[ this.count ];
			
		if( this.count > 0 )
		{
			CopyTo( index, array, 0, length );
		}

		return array;

	}

	/// <summary>
	/// Returns a subset of the collection's items as another dfList
	/// </summary>
	public dfList<T> GetRange( int index, int length )
	{
		var range = Obtain( length );
		CopyTo( 0, range.items, index, length );
		return range;
	}

	#endregion

	#region LINQ replacement methods (avoids allocating enumerators)

	/// <summary>
	/// Returns whether any items in the collection match the condition 
	/// defined by the predicate.
	/// </summary>
	/// <param name="predicate">A function to test each element for a condition.</param>
	/// <returns>true if any elements in the source sequence pass the test in the specified
	/// predicate; otherwise, false.</returns>
	public bool Any( Func<T, bool> predicate )
	{

		for( int i = 0; i < this.count; i++ )
		{
			if( predicate( this.items[ i ] ) )
				return true;
		}

		return false;

	}

	/// <summary>
	/// Returns the first element in the list. Throws an exception if the list is empty.
	/// </summary>
	/// <returns></returns>
	public T First()
	{
			
		if( this.count == 0 )
		{
			throw new IndexOutOfRangeException();
		}

		return this.items[ 0 ];

	}

	/// <summary>
	/// Returns the first element of the collection, or a default value 
	/// if the collection contains no elements.
	/// </summary>
	public T FirstOrDefault()
	{

		if( this.count > 0 )
			return this.items[ 0 ];

		return default( T );

	}

	/// <summary>
	/// Returns the first element of the collection matching the condition defined by 
	/// <paramref name="predicate"/>, or the default value for the element type if the 
	/// collection contains no elements.
	/// </summary>
	public T FirstOrDefault( Func<T, bool> predicate )
	{

		for( int i = 0; i < this.count; i++ )
		{
			if( predicate( items[ i ] ) )
				return items[ i ];
		}

		return default( T );

	}

	/// <summary>
	/// Returns the last element in the list. Throws an exception if the list is empty.
	/// </summary>
	public T Last()
	{

		if( this.count == 0 )
		{
			throw new IndexOutOfRangeException();
		}

		return this.items[ this.count - 1 ];

	}

	/// <summary>
	/// Returns the last element of the collection, or a default value 
	/// if the collection contains no elements.
	/// </summary>
	public T LastOrDefault()
	{

		if( this.count == 0 )
		{
			return default( T );
		}

		return this.items[ this.count - 1 ];


	}

	/// <summary>
	/// Returns the last element of the collection matching the condition defined by 
	/// <paramref name="predicate"/>, or the default value for the element type if the 
	/// collection contains no elements.
	/// </summary>
	public T LastOrDefault( Func<T, bool> predicate )
	{

		var result = default( T );
			
		for( int i = 0; i < this.count; i++ )
		{
			if( predicate( items[ i ] ) )
			{
				result = items[ i ];
			}
		}

		return result;

	}

	/// <summary>
	/// Returns a <see cref="dfList"/> list containing all elements
	/// of the collection matching the condition specified by <paramref name="predicate"/>
	/// </summary>
	public dfList<T> Where( Func<T, bool> predicate )
	{

		var result = dfList<T>.Obtain( this.count );

		for( int i = 0; i < this.count; i++ )
		{
			if( predicate( items[ i ] ) )
			{
				result.Add( items[ i ] );
			}
		}

		return result;

	}

	/// <summary>
	/// Returns the count of elements in the list that satisfy the
	/// condition defined by <paramref name="predicate"/>
	/// </summary>
	public int Matching( Func<T, bool> predicate )
	{

		var matching = 0;

		for( int i = 0; i < this.count; i++ )
		{
			if( predicate( items[ i ] ) )
				matching += 1;
		}

		return matching;

	}

	/// <summary>
	/// Projects each element of a sequence into a new form defined by <paramref name="selector"/>
	/// </summary>
	public dfList<TResult> Select<TResult>( Func<T, TResult> selector )
	{

		var result = dfList<TResult>.Obtain( this.count );

		for( int i = 0; i < this.count; i++ )
		{
			result.Add( selector( items[ i ] ) );
		}

		return result;

	}

	/// <summary>
	/// Returns a concatenated list containing all elements both lists
	/// </summary>
	public dfList<T> Concat( dfList<T> list )
	{

		var result = dfList<T>.Obtain( this.count + list.count );

		result.AddRange( this );
		result.AddRange( list );

		return result;

	}

	/// <summary>
	/// Converts all elements of the list to the specified target type
	/// </summary>
	public dfList<TResult> Convert<TResult>()
	{

		var result = dfList<TResult>.Obtain( this.count );

		for( int i = 0; i < this.count; i++ )
		{
			result.Add( (TResult)System.Convert.ChangeType( this.items[ i ], typeof( TResult ) ) );
		}

		return result;

	}

	/// <summary>
	/// Performs an action on each element of the list
	/// </summary>
	/// <param name="action">The action to be performed on each element</param>
	public void ForEach( Action<T> action )
	{
		var index = 0;
		while( index < this.Count )
		{
			action( items[ index++ ] );
		}
	}

	#endregion

	#region IEnumerable<T> implementation

	// NOTE: The IEnumerable<T> implementation here is horribly broken on iOS, and until
	// I can figure out a way to implement typed enumerators that do work on iOS, please
	// use a for(;;) loop instead of foreach(). Note that this may also apply to using
	// LINQ queries, which may use foreach() or an GetEnumerator() internally.

	/// <summary>
	/// Returns an IEnumerator instance that can be used to iterate through
	/// the elements in this list.
	/// </summary>
	public IEnumerator<T> GetEnumerator()
	{
		return PooledEnumerator.Obtain( this, null );
	}

	/// <summary>
	/// Returns an IEnumerator instance that can be used to iterate through
	/// the elements in this list.
	/// </summary>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return PooledEnumerator.Obtain( this, null );
	}

	#endregion

	#region IDisposable implementation 

	/// <summary>
	/// Releases the memory used by this object and returns it to the object pool
	/// </summary>
	public void Dispose()
	{
		Release();
	}

	#endregion

	#region Nested classes

	/// <summary>
	/// This custom enumerator class implements object pooling in order
	/// to reduce the number and frequency of memory allocations. It is 
	/// primarily expected to be used by framework and library code which
	/// treats the associated collection as an IEnumerable&lt;T&gt; rather
	/// than as a <see cref="dfList"/>. For instance, LINQ contains 
	/// several extension methods which will treat the collection as 
	/// an IEnumerable&lt;T&gt; and will create, use, and Dispose() of 
	/// the enumerator while performing the query. Similarly, many 
	/// third-party libraries will use a foreach() loop over a collection 
	/// rather than using for() with numeric indices, and the object pooling 
	/// implemented by this class will help to mitigate the number of 
	/// allocations typically caused by using such code.
	/// </summary>
	private class PooledEnumerator : IEnumerator<T>, IEnumerable<T>
	{

		#region Static variables

		private static Queue<PooledEnumerator> pool = new Queue<PooledEnumerator>();

		#endregion

		#region Private variables

		private dfList<T> list;
		private Func<T, bool> predicate;
		private int currentIndex;
		private T currentValue;
		private bool isValid = false;

		#endregion

		#region Pooling

		public static PooledEnumerator Obtain( dfList<T> list, Func<T, bool> predicate )
		{

			var enumerator = ( pool.Count > 0 ) ? pool.Dequeue() : new PooledEnumerator();
			enumerator.ResetInternal( list, predicate );

			return enumerator;

		}

		public void Release()
		{
			if( this.isValid )
			{
				this.isValid = false;
				pool.Enqueue( this );
			}
		}

		#endregion

		#region IEnumerator<T> Members

		public T Current
		{
			get
			{

				if( !this.isValid )
					throw new InvalidOperationException( "The enumerator is no longer valid" );

				return this.currentValue;

			}
		}

		#endregion

		#region Private utility methods

		private void ResetInternal( dfList<T> list, Func<T, bool> predicate )
		{
			this.isValid = true;
			this.list = list;
			this.predicate = predicate;
			this.currentIndex = 0;
			this.currentValue = default( T );
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			Release();
		}

		#endregion

		#region IEnumerator Members

		object IEnumerator.Current
		{
			get
			{
				return this.Current;
			}
		}

		public bool MoveNext()
		{

			if( !this.isValid )
				throw new InvalidOperationException( "The enumerator is no longer valid" );

			while( this.currentIndex < this.list.Count )
			{

				var valueAtIndex = this.list[ currentIndex++ ];
				if( predicate != null )
				{
					if( !predicate( valueAtIndex ) )
						continue;
				}

				this.currentValue = valueAtIndex;
				return true;

			}

			Release();

			this.currentValue = default( T );
			return false;

		}

		public void Reset()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IEnumerable Members

		public IEnumerator<T> GetEnumerator()
		{
			return this;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this;
		}

		#endregion

	}

	private class FunctorComparer : IComparer<T>, IDisposable
	{

		#region Static variables 

		private static Queue<FunctorComparer> pool = new Queue<FunctorComparer>();

		#endregion

		#region Private instance variables 

		private Comparison<T> comparison;

		#endregion

		#region Object pooling 

		public static FunctorComparer Obtain( Comparison<T> comparison )
		{
			var comparer = ( pool.Count > 0 ) ? pool.Dequeue() : new FunctorComparer();
			comparer.comparison = comparison;
			return comparer;
		}

		public void Release()
		{

			this.comparison = null;

			if( !pool.Contains( this ) )
			{
				pool.Enqueue( this );
			}

		}

		#endregion

		#region IComparer<T> implementation 

		public int Compare( T x, T y )
		{
			return this.comparison( x, y );
		}

		#endregion

		#region IDisposable implementation

		public void Dispose()
		{
			this.Release();
		}

		#endregion

	}

	#endregion

}
