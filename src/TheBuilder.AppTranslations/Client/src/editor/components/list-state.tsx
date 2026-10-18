import type { CSSProperties } from "react";
import { Button } from "../../bridge/uui/index.js";
import type { ListState as State } from "../state/list-state.js";

/**
 * What the list shows when it has no rows to show.
 *
 * Each of these ends in something to do, because an editor who reaches one of them is stuck, and a
 * sentence that only describes the situation leaves them stuck. The exception is having no source
 * configured, which an editor genuinely cannot fix: they are told who can, and the link appears
 * only for somebody able to follow it.
 */
export const ListState = ({
  state,
  term,
  filtered,
  canManageSources,
  onClear,
  onRetry,
}: {
  state: State;
  term: string;
  /** Whether anything is currently narrowing the list, so "clear" has something to clear. */
  filtered: boolean;
  canManageSources: boolean;
  onClear: () => void;
  onRetry: () => void;
}) => {
  if (state.kind === "loading") {
    return <Skeleton />;
  }

  if (state.kind === "error") {
    return (
      <div className="state" role="alert">
        <h2>Translations could not be loaded</h2>
        <p className="state__detail">{state.message}</p>
        <Button look="primary" onClick={onRetry}>
          Try again
        </Button>
      </div>
    );
  }

  if (state.kind === "no-sources") {
    return (
      <div className="state">
        <h2>No translations are available</h2>
        <p className="state__detail">
          An administrator needs to configure a translation source and synchronise it before
          anything appears here.
        </p>
        {/* Only for somebody who can actually act on it: pointing an editor at a settings page
            they cannot open is worse than saying nothing. */}
        {canManageSources ? (
          <Button
            onClick={() => {
              location.href = "/umbraco/section/settings";
            }}
          >
            Open settings
          </Button>
        ) : null}
      </div>
    );
  }

  return (
    <div className="state">
      <h2>{term ? <>No translations match “{term}”</> : "No translations match these filters"}</h2>
      <p className="state__detail">Try another search, or clear what you are filtering by.</p>
      {filtered ? (
        <Button look="primary" onClick={onClear}>
          Clear filters
        </Button>
      ) : null}
    </div>
  );
};

/**
 * Rows in outline, rather than a spinner or the word "Loading".
 *
 * The list arrives in the same shape every time, so drawing that shape while it loads means the
 * content appears in place instead of replacing something of a different size. That holds only if
 * the outline is the grid's shape to the pixel: the header band the real rows sit under, and one
 * line a row in the grid's own columns. Without the band, every outline row sat a header's height
 * above the row that replaced it, and two bars a row drew a second line no row has.
 */
const SKELETON_ROWS = Array.from({ length: 8 }, (_, row) => ({
  id: `skeleton-${row}`,
  // Uneven widths, so the outline reads as text rather than as a bar chart.
  key: `${45 + ((row * 7) % 35)}%`,
  value: `${35 + ((row * 13) % 45)}%`,
}));

/**
 * The key, the language being edited and the status, as `keyColumns` sizes them. Written out here
 * because the columns are built from the language being shown, which is not known until the list
 * has loaded; the comparison column is left out, since it is there only when one is chosen.
 */
const SKELETON_COLUMNS = "minmax(9rem, 0.85fr) minmax(0, 1.3fr) 7.5rem";

const Skeleton = () => (
  <div
    aria-label="Loading translations"
    className="state state--loading"
    role="status"
    style={{ "--columns": SKELETON_COLUMNS } as CSSProperties}
  >
    <span className="grid__row grid__row--head" />
    {SKELETON_ROWS.map((row) => (
      <span className="skeleton-row" key={row.id}>
        <span className="skeleton" style={{ inlineSize: row.key }} />
        <span className="skeleton" style={{ inlineSize: row.value }} />
      </span>
    ))}
  </div>
);
