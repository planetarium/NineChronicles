import { useQueries } from "@tanstack/react-query";
import axios from "axios";
import { useMemo } from "react";

interface Block { index: number }
interface Action { id: string, obsoleteAt: number }

const fetchLatestBlock = async () => {
  const response = await axios.get<Block>(`${import.meta.env.VITE_API_PATH}/api/blocks/latest`);
  return response.data;
};

const fetchActions = async () => {
  const response = await axios.get<Action[]>(import.meta.env.VITE_OBSOLETE_DATA_PATH);
  return response.data;
}

function App() {
  const [latestBlockQuery, actionsQuery] = useQueries({
    queries: [
      { queryKey: ['latestBlock'], queryFn: fetchLatestBlock },
      { queryKey: ['actions'], queryFn: fetchActions },
    ],
  });
  const isLoading = latestBlockQuery.isLoading || actionsQuery.isLoading;
  const isError = latestBlockQuery.isError || actionsQuery.isError;

  const currentBlock = useMemo(
    () => latestBlockQuery.data?.index ?? 0,
    [latestBlockQuery.data],
  );
  const actions = useMemo(
    () => actionsQuery.data?.sort((a, b) => b.obsoleteAt - a.obsoleteAt) ?? [],
    [actionsQuery.data],
  );

  if (isLoading) return <></>;
  if (isError) return <></>;

  const isObsoleted = (data: Action) => currentBlock > data.obsoleteAt;

  return (
    <div className="container mx-auto pt-16 pb-16">
      <div className="header pb-8">
        <h1 className="text-5xl font-bold mb-2">Dashboard</h1>
        <p>
          You can check NineChronicles actions that will be obsoleted soon here.
        </p>
      </div>

      <p className="text-xl mb-4">
        The latest block is <span className="font-extrabold">#{currentBlock}</span>
      </p>

      <div className="grid grid-cols-[120px_1fr_2fr] gap-2 text-lg">
        <>
          <p className="text-sm text-slate-500 font-bold">#Block Index</p>
          <p className="text-sm text-slate-500 font-bold">Action ID</p>
          <p className="text-sm text-slate-500 font-bold">Remain blocks</p>
        </>

        {actions.map((action) => (
          <>
            <p className={`font-bold ${isObsoleted(action) ? 'text-slate-400 line-through' : ''}`}>
              #{action.obsoleteAt}
            </p>
            <p className={isObsoleted(action) ? 'text-slate-400' : ''}>
              {action.id}
            </p>
            <p className={isObsoleted(action) ? 'text-slate-400' : ''}>
              {isObsoleted(action)
                ? <span className="text-sm">Obsoleted</span>
                : <span>{action.obsoleteAt - currentBlock} blocks remain</span>
              }
            </p>
          </>
        ))
        }
      </div>
    </div>
  );
}

export default App;
