// Add this function to handle click on Survey Fiber Route tasks
const handleSurveyTaskClick = (task) => {
  if (task && task.id) {
    // Navigate to survey details page
    window.location.href = `/SurveyTasks/Details/${task.id}`;
  }
};

// Then in your task rendering section, modify to make Survey tasks clickable
{tasks.map((task) => (
  <div 
    key={task.id} 
    className={`task-item ${activeTask === task ? 'active' : ''}`}
    onClick={() => {
      if (task.name === "SURVEY FIBER ROUTE") {
        handleSurveyTaskClick(task);
      } else {
        handleTaskClick(task);
      }
    }}
  >
    <div className="task-name">
      {task.name}
      {task.name === "SURVEY FIBER ROUTE" && (
        <span className="ms-2 text-primary">
          <i className="bi bi-info-circle" title="Click for details"></i>
        </span>
      )}
    </div>
    <div className="task-status">
      <span className={`status-indicator ${task.status.toLowerCase()}`}></span>
      {task.status}
    </div>
  </div>
))}