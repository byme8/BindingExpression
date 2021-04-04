# Binding Expression

The goal of this library to provide easy way to generate binding path via expression trees for libraries like [CSharpForMarkup](https://github.com/VincentH-Net/CSharpForMarkup).

## How the ` CSharpForMarkup ` approach works

Lets suppose you need to create a list of items on your page and you decied to use ` CSharpForMarkup `. The official way may look like that:

``` cs 
// ...

Content = new ListView()
    .Bind(ListView.ItemSourceProperty, nameof(ViewModel.Items))
    .Bind(ListView.ItemTemplateProperty, () => 
      new DataTemplate(() => new ViewCell 
        { 
          View = new Label { TextColor = Color.RoyalBlue }
                    .Bind(Label.TextProperty, nameof(ViewModel.Item.Text))
                    .TextCenterHorizontal()
                    .TextCenterVertical() 
         }))
// ...
```

It is pretty simple, but a bit ugly way to define a ListView. We can do better.
Lets create a static class ` XamarinElements ` and add a static method to it:

``` cs 

public static class XamarinElements
{
    public static ListView ListView<T>(string path, Func<T, View> itemTemplate = null)
    {
        return new ListView
        {
            ItemTemplate = new DataTemplate(() => new ViewCell {View = itemTemplate?.Invoke()})
        }.Bind(ListView.ItemsSourceProperty, path);
    }
}
```

Now we can use it like that:
``` cs
// ...
using static XamarinElements;
// ...

Content = ListView(nameof(ViewModel.Items), () => 
          new Label { TextColor = Color.RoyalBlue }
            .Bind(Label.TextProperty, nameof(ViewModel.Item.Text))
            .TextCenterHorizontal()
            .TextCenterVertical() 
         )
// ...
```

It looks much better but there are some issues with it. For example, what to do if you want to specify a deep binding path like 'Item.Date.Hour'? 
I have not found any good solution and decided to investigate what can I do.

The first idea was to use ` Expression<Func<TValue>> `.
In this case the full sample will look like that:
``` cs

public static class XamarinElements
{
  public static ListView ListView<T>(Expression<Func<IEnumerable<T>>> path, Func<T, View> itemTemplate = null)
  {
      var pathFromExpression = path.GetBindingPath();
      return new ListView
      {
          ItemTemplate = new DataTemplate(() => new ViewCell {View = itemTemplate?.Invoke(default)})
      }.Bind(ListView.ItemsSourceProperty, pathFromExpression);
  }
  
  public static TView Bind<TView, T>(this TView view, BindableProperty property, Expression<Func<T>> expression)
    where TView: BindableObject
  {
      view.Bind(property, expression.GetBindingPath());
      return view;
  }
}

// ...
using static XamarinElements;
// ...

Content = ListView(() => ViewModel.Items, () => 
          new Label { TextColor = Color.RoyalBlue }
            .Bind(Label.TextProperty, o => o.Item.Date.Hour))
            .TextCenterHorizontal()
            .TextCenterVertical() 
         )
// ...

``` 
Now it looks good and we can create a binding of any length. Meanwhile it has issues too. For example, if you use it like that it will break at runtime:
``` cs

// ...
using static XamarinElements;
// ...

Content = ListView(() => ViewModel.Items, () => 
          new Label { TextColor = Color.RoyalBlue }
            .Bind(Label.TextProperty, o => o.Item.Date.Hour + 1)) // pay attention to this line
            .TextCenterHorizontal()
            .TextCenterVertical() 
         )
// ...

```
To prevent it from happening we can develop a Roslyn analyzer that will look through all expression bindings and generate an error when we are using the expression in the wrong way.
And this library handles it. 

To make the analyzer work we will need to install a nuget package ` BindingExpression ` and update our ` XamarinElements ` to look like that:
``` cs

public static class XamarinElements
{
  public static ListView ListView<T>(
    [BindingExpression]Expression<Func<IEnumerable<T>>> path,  // check this like
    Func<T, View> itemTemplate = null)
  {
      var pathFromExpression = path.GetBindingPath();
      return new ListView
      {
          ItemTemplate = new DataTemplate(() => new ViewCell {View = itemTemplate?.Invoke(default)})
      }.Bind(ListView.ItemsSourceProperty, pathFromExpression);
  }
  
  public static TView Bind<TView, T>(
    this TView view, BindableProperty property, 
    [BindingExpression]Expression<Func<T>> expression) // check this like
    where TView: BindableObject
  {
      view.Bind(property, expression.GetBindingPath());
      return view;
  }
}

```
The ` BindingExpression ` attribute works as marker for analyzer. When analyzer finds the attribute it will check all usages and if expression is too complex the error will be generated.
After these tweaks our broken sample will generete an error:
``` cs

// ...
using static XamarinElements;
// ...

Content = ListView(() => ViewModel.Items, () => 
          new Label { TextColor = Color.RoyalBlue }
            .Bind(Label.TextProperty, o => o.Item.Date.Hour + 1)) // error here
            .TextCenterHorizontal()
            .TextCenterVertical() 
         )
// ...
``` 



