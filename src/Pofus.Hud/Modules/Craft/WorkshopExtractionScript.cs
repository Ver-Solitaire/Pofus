namespace Pofus.Hud.Modules.Craft;

/// <summary>
/// The scripts run inside the loaded workshop page to read its contents.
///
/// Split in two because <c>ExecuteScriptAsync</c> returns an expression's value,
/// not the resolution of a promise: <see cref="Kickoff"/> starts the lookup and
/// parks the answer on <c>window</c>, and <see cref="Poll"/> is called until it
/// appears.
///
/// It asks the page's own API, whose payload is shaped:
/// <code>
/// { "crafts": [ { "item_id": 1450, "quantity": 1,
///                 "item": { "ingredients": [ { "id": 6458, "count": 1, "name": "Kobalite" } ] } } ] }
/// </code>
/// Everything needed is already there — ingredient identity, label and per-unit
/// count — so no external item database is involved. The script only transcribes
/// that structure; the arithmetic is done in C# where it is unit-tested.
///
/// Going through the API rather than the rendered page also side-steps the
/// cookie banner DofusBook draws over the document, so nothing has to be shown
/// to the user.
/// </summary>
internal static class WorkshopExtractionScript
{
    public const string Kickoff = """
(() => {
  window.__pofusWorkshop = undefined;

  const label = v => typeof v === 'string' ? v : (v && typeof v === 'object' ? (v.fr || v.en || null) : null);

  const readCrafts = (payload) => {
    const list = Array.isArray(payload) ? payload : (payload && payload.crafts);
    if (!Array.isArray(list)) return [];
    const crafts = [];
    for (const craft of list) {
      if (!craft || typeof craft !== 'object') continue;
      const item = craft.item || {};
      const ingredients = [];
      for (const ing of (item.ingredients || [])) {
        const id = ing && (ing.id ?? ing.item_id);
        const name = label(ing && ing.name);
        const count = Number(ing && (ing.count ?? ing.quantity ?? ing.qty));
        const picture = Number(ing && ing.picture);
        if (Number.isInteger(id) && id > 0 && name && count > 0) {
          ingredients.push({
            id: id, name: String(name), count: count,
            picture: Number.isInteger(picture) && picture > 0 ? picture : 0
          });
        }
      }
      if (!ingredients.length) continue;
      const picture = Number(item.picture);
      crafts.push({
        itemId: Number(craft.item_id ?? item.id) || 0,
        // item.name is the equipment itself; cloth_name is its panoply, which
        // would label several different pieces identically.
        name: String(label(item.name) || label(item.cloth_name) || label(craft.folder_name) || 'Équipement'),
        quantity: Number(craft.quantity) > 0 ? Number(craft.quantity) : 1,
        picture: Number.isInteger(picture) && picture > 0 ? picture : 0,
        ingredients: ingredients
      });
    }
    return crafts;
  };

  (async () => {
    try {
      const m = location.pathname.match(/membre\/([^\/]+)/);
      if (!m) { window.__pofusWorkshop = ''; return; }

      for (const path of ['/api/crafts/' + m[1], '/api/crafts/' + m[1] + '/legacy']) {
        try {
          const r = await fetch(path, { credentials: 'include', headers: { accept: 'application/json' } });
          if (!r.ok) continue;
          const crafts = readCrafts(await r.json());
          if (crafts.length) {
            window.__pofusWorkshop = JSON.stringify({ v: 2, source: 'dofusbook', url: location.href, crafts: crafts });
            return;
          }
        } catch (e) { /* try the next candidate */ }
      }
      window.__pofusWorkshop = '';
    } catch (e) {
      window.__pofusWorkshop = '';
    }
  })();
  return 'started';
})();
""";

    /// <summary>Returns the envelope once ready, an empty string on definitive
    /// failure, or null while the lookup is still running.</summary>
    public const string Poll = "window.__pofusWorkshop === undefined ? null : window.__pofusWorkshop;";
}
